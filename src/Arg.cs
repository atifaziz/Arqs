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

    partial interface IArg
    {
        string  Name        { get; }
        string  Description { get; }

        IReader CreateReader();
    }

    partial class ArgInfo
    {
        public ArgInfo(string name, string description = null)
        {
            Name          = name ?? throw new ArgumentNullException(nameof(name));
            Description   = description;
        }

        public string   Name        { get; }
        public string   Description { get; }

        public ArgInfo WithName(string value)
            => value == null ? throw new ArgumentNullException(nameof(value))
             : value == Name ? this
             : UpdateCore(value, Description);

        public ArgInfo WithDescription(string value) =>
            value == Description ? this : UpdateCore(Name, value);

        protected virtual ArgInfo UpdateCore(string name, string description) =>
            new ArgInfo(name, description);

        public override string ToString() => Name + String.ConcatAll(": " + Description);

        public Arg<T> ToArg<T>(IParser<T> parser, Func<IReader> readerFactory, Func<IReader, T> binder) =>
            new Arg<T>(this, parser, readerFactory, binder);
    }

    partial class Arg<T> : IArg, IArgBinder<T>
    {
        readonly ArgInfo _info;
        readonly Func<IReader> _readerFactory;
        readonly Func<IReader, T> _binder;

        public Arg(ArgInfo info, IParser<T> parser, Func<IReader> readerFactory, Func<IReader, T> binder)
        {
            _info = info ?? throw new ArgumentNullException(nameof(info));
            _readerFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public string  Name        => _info.Name;
        public string  Description => _info.Description;

        public IParser<T> Parser { get; }

        public IReader CreateReader() => _readerFactory();

        Arg<T> WithInfo(ArgInfo value) =>
            value == _info ? this : new Arg<T>(value, Parser, _readerFactory, _binder);

        public Arg<T> WithName(string value) =>
            WithInfo(_info.WithName(value));

        public Arg<T> WithDescription(string value) =>
            WithInfo(_info.WithDescription(value));

        public T Bind(Func<IArg, IReader> source) =>
            _binder(source(this));

        public void Inspect(ICollection<IArg> args) => args.Add(this);
    }

    partial class ListArg<T> : IArg, IArgBinder<ImmutableArray<T>>
    {
        readonly Arg<T> _arg;

        public ListArg(Arg<T> arg)
        {
            _arg = arg ?? throw new ArgumentNullException(nameof(arg));
        }

        public string Name => _arg.Name;
        public string Description => _arg.Description;

        public IParser<T> ItemParser => _arg.Parser;

        public IReader CreateReader() => new Reader(_arg);

        ListArg<T> WithArg(Arg<T> value) =>
            value == _arg ? this : new ListArg<T>(value);

        public ListArg<T> WithName(string value) =>
            WithArg(_arg.WithName(value));

        public ListArg<T> WithDescription(string value) =>
            WithArg(_arg.WithDescription(value));

        public ImmutableArray<T> Bind(Func<IArg, IReader> source) =>
            ((Reader)source(this)).Value.ToImmutable();

        public void Inspect(ICollection<IArg> args) => args.Add(this);

        sealed class Reader : IReader<ImmutableArray<T>.Builder>
        {
            readonly Arg<T> _arg;

            public Reader(Arg<T> arg) => _arg = arg;

            public bool HasValue => true;

            public ImmutableArray<T>.Builder Value { get; } = ImmutableArray.CreateBuilder<T>();

            object IReader.Value => Value;

            public bool Read(IEnumerator<string> arg)
            {
                var reader = _arg.CreateReader();
                if (!reader.Read(arg))
                    return false;
                Value.Add(_arg.Bind(_ => reader));
                return true;
            }
        }
    }

    static partial class Arg
    {
        public static Arg<bool> Flag(string name) =>
            new ArgInfo(name).ToArg(Parser.Create<bool>(_ => throw new NotSupportedException()), Reader.Flag, r => r.HasValue);

        public static Arg<T> Value<T>(string name, T @default, IParser<T> parser) =>
            new ArgInfo(name).ToArg(parser, () => Reader.Value(parser), r => r.HasValue ? ((IReader<T>)r).Value : @default);

        public static Arg<T> Value<T>(string name, IParser<T> parser) =>
            Value(name, default, parser);

        public static ListArg<T> List<T>(this Arg<T> arg) =>
            new ListArg<T>(arg);
    }

    partial interface IReader
    {
        bool HasValue { get; }
        object Value { get; }
        bool Read(IEnumerator<string> arg);
    }

    partial interface IReader<out T> : IReader
    {
        new T Value { get; }
    }

    partial interface IArgBinder<out T>
    {
        T Bind(Func<IArg, IReader> source);
        void Inspect(ICollection<IArg> args);
    }

    static partial class Reader
    {
        public static IReader<T> Value<T>(IParser<T> parser) =>
            new ValueReader<T>(default, (_, arg) => !arg.MoveNext() ? default : parser.Parse(arg.Current));

        public static IReader<int> Flag() =>
            new ValueReader<int>(0, (count, _) => ParseResult.Success(count + 1));

        sealed class ValueReader<T> : IReader<T>
        {
            public bool HasValue { get; private set; }
            public T Value { get; private set; }

            object IReader.Value => Value;

            readonly Func<T, IEnumerator<string>, ParseResult<T>> _reader;

            public ValueReader(T initial, Func<T, IEnumerator<string>, ParseResult<T>> reader)
            {
                Value = initial;
                _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            }

            public bool Read(IEnumerator<string> arg)
            {
                switch (_reader(Value, arg))
                {
                    case (true, var value):
                        HasValue = true;
                        Value = value;
                        return true;
                    default:
                        Value = default;
                        return false;
                }
            }
        }
    }

    static partial class ArgBinder
    {
        public static readonly IArgBinder<Unit> Nop = Create<Unit>(_ => default, delegate {});

        public static IList<IArg> Inspect<T>(this IArgBinder<T> binder)
        {
            var infos = new List<IArg>();
            binder.Inspect(infos);
            return infos;
        }

        public static IArgBinder<(T, U)> Zip<T, U>(this IArgBinder<T> first, IArgBinder<U> second) =>
            Create(bindings => (first.Bind(bindings), second.Bind(bindings)),
                   args     => { first.Inspect(args); second.Inspect(args); });

        public static (T Result, ImmutableArray<string> Tail)
            Bind<T>(this IArgBinder<T> binder, params string[] args)
        {
            var infos = new List<IArg>();
            binder.Inspect(infos);
            var values = new IReader[infos.Count];
            for (var i = 0; i < infos.Count; i++)
                values[i] = infos[i].CreateReader();
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
                    if (!values[i].Read(e))
                        throw new Exception("Invalid value for argument: " + name);
                }
            }

            return (binder.Bind(info => values[infos.IndexOf(info)]), tail.ToImmutableArray());
        }

        public static IArgBinder<T> Create<T>(Func<Func<IArg, IReader>, T> binder, Action<ICollection<IArg>> inspector) =>
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
            readonly Func<Func<IArg, IReader>, T> _binder;
            readonly Action<ICollection<IArg>> _inspector;

            public DelegatingArgBinder(Func<Func<IArg, IReader>, T> binder,
                                       Action<ICollection<IArg>> inspector)
            {
                _binder = binder;
                _inspector = inspector;
            }

            public T Bind(Func<IArg, IReader> source) =>
                _binder(source);

            public void Inspect(ICollection<IArg> args) =>
                _inspector(args);
        }
    }
}
