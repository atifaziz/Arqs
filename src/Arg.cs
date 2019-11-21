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
        public ArgInfo(string name, string description = null,
                       bool isFlag = false, bool isBreaker = false)
        {
            Name        = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            IsFlag      = isFlag;
            IsBreaker   = isBreaker;
        }

        public string Name        { get; }
        public string Description { get; }
        public bool   IsFlag      { get; }
        public bool   IsBreaker   { get; }

        public ArgInfo WithName(string value)
            => value == null ? throw new ArgumentNullException(nameof(value))
             : value == Name ? this
             : UpdateCore(value, Description, IsFlag, IsBreaker);

        public ArgInfo WithDescription(string value) =>
            value == Description ? this : UpdateCore(Name, value, IsFlag, IsBreaker);

        public ArgInfo WithIsBreaker(bool value) =>
            value == IsBreaker ? this : UpdateCore(Name, Description, IsFlag, value);

        protected virtual ArgInfo UpdateCore(string name, string description, bool isFlag, bool isBreaker) =>
            new ArgInfo(name, description, isFlag, isBreaker);

        public override string ToString() => Name + String.ConcatAll(": " + Description);

        public Arg<T> ToArg<T>(Func<IArgSource, ArgInfo, T> binder) => new Arg<T>(this, binder);
    }

    partial class Arg<T> : IArgBinder<T>
    {
        readonly ArgInfo _info;

        public Arg(ArgInfo info, Func<IArgSource, ArgInfo, T> binder)
        {
            _info = info ?? throw new ArgumentNullException(nameof(info));
            Binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public string Name        => _info.Name;
        public string Description => _info.Description;
        public bool   IsFlag      => _info.IsFlag;
        public bool   IsBreaker   => _info.IsBreaker;

        public Func<IArgSource, ArgInfo, T> Binder { get; }

        public Arg<TArg> WithBinder<TArg>(Func<IArgSource, ArgInfo, TArg> binder) =>
            new Arg<TArg>(_info, binder);

        Arg<T> WithInfo(ArgInfo value) =>
            value == _info ? this : new Arg<T>(value, Binder);

        public Arg<T> WithName(string value) =>
            WithInfo(_info.WithName(value));

        public Arg<T> WithDescription(string value) =>
            WithInfo(_info.WithDescription(value));

        public Arg<T> WithIsBreaker(bool value) =>
            WithInfo(_info.WithIsBreaker(value));

        public Arg<T> Break() => WithIsBreaker(true);

        public T Bind(IArgSource source) =>
            Binder(source, _info);

        public void Inspect(ICollection<ArgInfo> args) => args.Add(_info);
    }

    static partial class Arg
    {
        public static Arg<bool> Flag(string name) =>
            new ArgInfo(name, isFlag: true).ToArg((source, info) => source.Lookup(info) != null);

        public static Arg<T> Value<T>(string name, T @default, IParser<T> parser) =>
            new ArgInfo(name).ToArg((source, info) => source.Lookup(info) is string s ? parser.Parse(s) is (true, var v) ? v : throw source.InvalidArgValue(info, s) : @default);

        public static Arg<T> Once<T>(this Arg<T> arg) =>
            arg.WithBinder((source, info) =>
            {
                var result = arg.Binder(source, info);
                return source.Lookup(info) == null ? result : throw new Exception($"Argument \"{info.Name}\" was specified more than once."); ;
            });

        public static Arg<T> Value<T>(string name, IParser<T> parser) =>
            Value(name, default, parser);

        public static Arg<ImmutableArray<T>> List<T>(this Arg<T> arg) =>
            arg.WithBinder((source, info) =>
            {
                var tokens = new List<string>();
                while (source.Lookup(info) is string s)
                    tokens.Add(s);
                return ImmutableArray.CreateRange(from t in tokens
                                                  select arg.Bind(new SingletonArgSource(source, t)));
            });

        sealed class SingletonArgSource : IArgSource
        {
            readonly IArgSource _other;
            string _value;

            public SingletonArgSource(IArgSource other, string value) =>
                (_other, _value) = (other, value);

            public string Lookup(ArgInfo arg)
            {
                var value = _value;
                _value = null;
                return value;
            }

            public Exception InvalidArgValue(ArgInfo arg, string text) =>
                _other.InvalidArgValue(arg, text);
        }
    }

    partial interface IArgSource
    {
        string Lookup(ArgInfo arg);
        Exception InvalidArgValue(ArgInfo arg, string text);
    }

    partial class ArgSource : IArgSource
    {
        public static readonly IArgSource Empty = new EmptyArgSource();

        sealed class EmptyArgSource : IArgSource
        {
            public string Lookup(ArgInfo arg) => null;
            public Exception InvalidArgValue(ArgInfo arg, string text) => throw new NotImplementedException();
        }

        readonly (bool Taken, string Text)[] _args;
        bool _breaking;

        public ArgSource(ICollection<string> args)
        {
            _args = new (bool Taken, string Text)[args.Count + 1];
            var a = _args.AsSpan(1);
            var i = 0;
            foreach (var arg in args)
                a[i++].Text = arg;
        }

        static readonly Predicate<string> Mismatch = _ => false;

        public string Lookup(ArgInfo arg)
        {
            static string Dash(string s) =>
                s == null ? null : (s.Length == 1 ? "-" : "--") + s;

            var lf = Dash(arg.Name) is string ln ? s => s == ln : Mismatch;

            var result = arg.IsFlag ? LookupFlag(lf) : Lookup(lf);

            if (_breaking)
                return null;

            if (arg.IsBreaker && result != null)
                _breaking = true;

            return result;

            string Lookup(Predicate<string> predicate)
            {
                for (var i = 1; i < _args.Length; i++)
                {
                    ref var prev = ref _args[i - 1];
                    ref var arg = ref _args[i];
                    if (!prev.Taken && predicate(prev.Text))
                    {
                        prev.Taken = true;
                        arg.Taken = true;
                        return arg.Text;
                    }
                }

                return null;
            }

            string LookupFlag(Predicate<string> predicate)
            {
                foreach (ref var arg in _args.AsSpan(1))
                {
                    if (!arg.Taken && predicate(arg.Text))
                    {
                        arg.Taken = true;
                        return arg.Text;
                    }
                }

                return null;
            }
        }

        public Exception InvalidArgValue(ArgInfo arg, string text) =>
            throw new Exception($"Invalid value for argument \"{arg.Name}\": {text}");

        public IEnumerable<string> Unused =>
            from arg in _args.Skip(1)
            where !arg.Taken
            select arg.Text;
    }

    partial interface IArgBinder<out T>
    {
        T Bind(IArgSource source);
        void Inspect(ICollection<ArgInfo> args);
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
            Create(
                source => (first.Bind(source), second.Bind(source)),
                args   => { first.Inspect(args); second.Inspect(args); });

        public static (T Result, ImmutableArray<string> Tail)
            Bind<T>(this IArgBinder<T> binder, params string[] args)
        {
            var source = new ArgSource(args);
            var result = binder.Bind(source);
            return (result, ImmutableArray.CreateRange(source.Unused));
        }

        public static IArgBinder<T> Create<T>(Func<IArgSource, T> binder, Action<ICollection<ArgInfo>> inspector) =>
            new DelegatingArgBinder<T>(binder, inspector);

        public static IArgBinder<U> Select<T, U>(this IArgBinder<T> binder, Func<T, U> f) =>
            Create(args => f(binder.Bind(args)), binder.Inspect);

        public static IArgBinder<U> SelectMany<T, U>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f) =>
            Create(args => f(binder.Bind(args)).Bind(args),
                   args =>
                   {
                       binder.Inspect(args);
                       f(binder.Bind(ArgSource.Empty)).Inspect(args);
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
            readonly Func<IArgSource, T> _binder;
            readonly Action<ICollection<ArgInfo>> _inspector;

            public DelegatingArgBinder(Func<IArgSource, T> binder, Action<ICollection<ArgInfo>> inspector)
            {
                _binder = binder;
                _inspector = inspector;
            }

            public T Bind(IArgSource source) =>
                _binder(source);

            public void Inspect(ICollection<ArgInfo> args) =>
                _inspector(args);
        }
    }
}
