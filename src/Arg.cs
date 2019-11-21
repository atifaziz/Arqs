#region Copyright (c) 2019 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace Largs
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Unit = System.ValueTuple;

    partial class ArgInfo
    {
        public ArgInfo(IReader reader,
                       string name, string description = null,
                       bool isFlag = false, bool isBreaker = false)
        {
            Reader      = reader ?? throw new ArgumentNullException(nameof(reader));
            Name        = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            IsFlag      = isFlag;
            IsBreaker   = isBreaker;
        }

        public IReader  Reader      { get; }
        public string   Name        { get; }
        public string   Description { get; }
        public bool     IsFlag      { get; }
        public bool     IsBreaker   { get; }

        public ArgInfo WithName(string value)
            => value == null ? throw new ArgumentNullException(nameof(value))
             : value == Name ? this
             : UpdateCore(value, Description, IsFlag, IsBreaker);

        public ArgInfo WithDescription(string value) =>
            value == Description ? this : UpdateCore(Name, value, IsFlag, IsBreaker);

        public ArgInfo WithIsBreaker(bool value) =>
            value == IsBreaker ? this : UpdateCore(Name, Description, IsFlag, value);

        protected virtual ArgInfo UpdateCore(string name, string description, bool isFlag, bool isBreaker) =>
            new ArgInfo(Reader, name, description, isFlag, isBreaker);

        public override string ToString() => Name + String.ConcatAll(": " + Description);

        public Arg<T> ToArg<T>(Func<object, T> binder) => new Arg<T>(this, binder);
    }

    partial class Arg<T> : IArgBinder<T>
    {
        readonly ArgInfo _info;
        readonly Func<object, T> _binder;

        public Arg(ArgInfo info, Func<object, T> binder)
        {
            _info = info ?? throw new ArgumentNullException(nameof(info));
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public IReader Reader      => _info.Reader;
        public string  Name        => _info.Name;
        public string  Description => _info.Description;
        public bool    IsFlag      => _info.IsFlag;
        public bool    IsBreaker   => _info.IsBreaker;

        public Arg<TArg> WithReader<TArg>(IReader reader, Func<object, TArg> binder) =>
            new Arg<TArg>(new ArgInfo(reader, Name, Description, IsFlag, IsBreaker), binder);

        Arg<T> WithInfo(ArgInfo value) =>
            value == _info ? this : new Arg<T>(value, _binder);

        public Arg<T> WithName(string value) =>
            WithInfo(_info.WithName(value));

        public Arg<T> WithDescription(string value) =>
            WithInfo(_info.WithDescription(value));

        public Arg<T> WithIsBreaker(bool value) =>
            WithInfo(_info.WithIsBreaker(value));

        public Arg<T> Break() => WithIsBreaker(true);

        public T Bind(Func<ArgInfo, object> source) =>
            _binder(source(_info));

        public void Inspect(ICollection<ArgInfo> args) => args.Add(_info);
    }

    static partial class Arg
    {
        public static Arg<bool> Flag(string name) =>
            new ArgInfo(Reader.Flag(), name, isFlag: true).ToArg(v => (bool?)v ?? false);

        public static Arg<T> Value<T>(string name, T @default, IParser<T> parser) =>
            new ArgInfo(Reader.Value(parser), name).ToArg(v => v == null ? @default : (T)v);

        public static Arg<T> Value<T>(string name, IParser<T> parser) =>
            Value(name, default, parser);

        public static Arg<ImmutableArray<T>> List<T>(this Arg<T> arg) =>
            arg.WithReader(new ArrayReader<T>(arg.Reader), v => (ImmutableArray<T>?)v ?? ImmutableArray<T>.Empty);

        sealed class ArrayReader<T> : IReader
        {
            readonly IReader _reader;
            ImmutableArray<T> _array = ImmutableArray<T>.Empty;

            public ArrayReader(IReader reader) => _reader = reader;

            public ParseResult<object> Read(IEnumerator<string> arg) =>
                _reader.Read(arg) switch
                {
                    (true, var value) => ParseResult.Success<object>(_array = _array.Add((T)value)),
                    _ => default
                };
        }
    }

    partial interface IReader
    {
        ParseResult<object> Read(IEnumerator<string> arg);
    }

    partial interface IArgBinder<out T>
    {
        T Bind(Func<ArgInfo, object> source);
        void Inspect(ICollection<ArgInfo> args);
    }

    static partial class Reader
    {
        public static IReader Value(IParser parser) =>
            new DelegatingReader(arg => !arg.MoveNext() ? default : parser.Parse(arg.Current));

        static readonly object TrueObject = true;

        public static IReader Flag() =>
            new DelegatingReader(arg => ParseResult.Success(TrueObject));

        sealed class DelegatingReader : IReader
        {
            readonly Func<IEnumerator<string>, ParseResult<object>> _reader;

            public DelegatingReader(Func<IEnumerator<string>, ParseResult<object>> reader) =>
                _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            public ParseResult<object> Read(IEnumerator<string> arg) => _reader(arg);
        }
    }

    static partial class ArgBinder
    {
        public static readonly IArgBinder<Unit> Nop = Create<Unit>(_ => default, delegate {});

        public static IList<ArgInfo> Inspect<T>(this IArgBinder<T> binder)
        {
            var infos = new List<ArgInfo>();
            binder.Inspect(infos);
            return infos;
        }

        public static IArgBinder<(T, U)> Zip<T, U>(this IArgBinder<T> first, IArgBinder<U> second) =>
            Create(bindings => (first.Bind(bindings), second.Bind(bindings)),
                   args     => { first.Inspect(args); second.Inspect(args); });

        public static (T Result, ImmutableArray<string> Tail)
            Bind<T>(this IArgBinder<T> binder, params string[] args)
        {
            var infos = new List<ArgInfo>();
            binder.Inspect(infos);
            var values = new object[infos.Count];
            using var e = args.AsEnumerable().GetEnumerator();
            var tail = new List<string>();
            while (e.MoveNext())
            {
                var arg = e.Current;
                var name = arg.StartsWith("--", StringComparison.Ordinal) ? arg.Substring(2)
                         : arg.Length > 1 && arg[0] == '-' ? arg.Substring(1)
                         : null;
                if (name == null)
                {
                    tail.Add(arg);
                }
                else
                {
                    var i = infos.FindIndex(e => e.Name == name);
                    var (success, value) = infos[i].Reader.Read(e);
                    if (!success)
                        throw new Exception("Invalid value for argument: " + name);
                    values[i] = value;
                }
            }

            return (binder.Bind(info => values[infos.IndexOf(info)]), tail.ToImmutableArray());
        }

        public static IArgBinder<T> Create<T>(Func<Func<ArgInfo, object>, T> binder, Action<ICollection<ArgInfo>> inspector) =>
            new DelegatingArgBinder<T>(binder, inspector);

        public static IArgBinder<U> Select<T, U>(this IArgBinder<T> binder, Func<T, U> f) =>
            Create(bindings => f(binder.Bind(bindings)), binder.Inspect);

        public static IArgBinder<U> SelectMany<T, U>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f) =>
            Create(bindings => f(binder.Bind(bindings)).Bind(bindings),
                   args =>
                   {
                       binder.Inspect(args);
                       f(binder.Bind(delegate { throw new InvalidOperationException(); })).Inspect(args);
                   });

        public static IArgBinder<V> SelectMany<T, U, V>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f, Func<T, U, V> g) =>
            binder.Select(t => f(t).Select(u => g(t, u))).SelectMany(pv => pv);

        public static IArgBinder<V> Join<T, U, K, V>(this IArgBinder<T> first, IArgBinder<U> second,
                                                     Func<T, K> unused1, Func<T, K> unused2,
                                                     Func<T, U, V> resultSelector) =>
            from ab in first.Zip(second)
            select resultSelector(ab.Item1, ab.Item2);

        sealed class DelegatingArgBinder<T> : IArgBinder<T>
        {
            readonly Func<Func<ArgInfo, object>, T> _binder;
            readonly Action<ICollection<ArgInfo>> _inspector;

            public DelegatingArgBinder(Func<Func<ArgInfo, object>, T> binder,
                                       Action<ICollection<ArgInfo>> inspector)
            {
                _binder = binder;
                _inspector = inspector;
            }

            public T Bind(Func<ArgInfo, object> source) =>
                _binder(source);

            public void Inspect(ICollection<ArgInfo> args) =>
                _inspector(args);
        }
    }
}
