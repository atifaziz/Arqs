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
    using System.Linq;

    partial class ArgInfo
    {
        public ArgInfo(string name, string shortName = null, string otherName = null,
                       string description = null)
        {
            Name        = name ?? throw new ArgumentNullException(nameof(name));
            ShortName   = shortName;
            OtherName   = otherName;
            Description = description;
        }

        public string Name        { get; }
        public string ShortName   { get; }
        public string OtherName   { get; }
        public string Description { get; }

        public ArgInfo WithName(string value)
            => value == null ? throw new ArgumentNullException(nameof(value))
             : value == Name ? this
             : UpdateCore(value, ShortName, OtherName, Description);

        public ArgInfo WithShortName(string value) =>
            value == ShortName ? this : UpdateCore(Name, value, OtherName, Description);

        public ArgInfo WithOtherName(string value) =>
            value == OtherName ? this : UpdateCore(Name, ShortName, value, Description);

        public ArgInfo WithDescription(string value) =>
            value == Description ? this : UpdateCore(Name, ShortName, OtherName, value);

        protected virtual ArgInfo UpdateCore(string name, string shortName, string otherName,
                                             string description) =>
            new ArgInfo(name, shortName, otherName, description);

        public override string ToString() =>
            string.Join("|", Name, ShortName, OtherName)
            + String.ConcatAll(": " + Description);
    }

    partial class Arg<T> : IArgBinder<T>
    {
        readonly ArgInfo _info;

        public Arg(ArgInfo info, IArgBinder<T> binder)
        {
            _info = info ?? throw new ArgumentNullException(nameof(info));
            Binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public string Name        => _info.Name;
        public string ShortName   => _info.ShortName;
        public string OtherName   => _info.OtherName;
        public string Description => _info.Description;

        public IArgBinder<T> Binder { get; }

        public IArgBinder<TArg> WithBinder<TArg>(IArgBinder<TArg> value) =>
            new Arg<TArg>(_info, value);

        IArgBinder<T> WithInfo(ArgInfo value) =>
            value == _info ? this : new Arg<T>(_info, Binder);

        public IArgBinder<T> WithName(string value) =>
            WithInfo(_info.WithName(value));

        public IArgBinder<T> WithShortName(string value) =>
            WithInfo(_info.WithShortName(value));

        public IArgBinder<T> WithOtherName(string value) =>
            WithInfo(_info.WithOtherName(value));

        public IArgBinder<T> WithDescription(string value) =>
            WithInfo(_info.WithDescription(value));

        public T Bind(IArgSource source) =>
            source.Lookup(Name, null, null) is string s ? Binder.Bind(s) : throw new Exception();

        public void Inspect(ICollection<ArgInfo> args) => args.Add(_info);
    }

    static partial class Arg
    {
        public static IArgBinder<T> Require<T>(string name, IParser<T> parser) =>
            ArgBinder.Create(source => source.Lookup(name, null, null) is string s ? parser.Parse(s) : throw new Exception(),
                             args => args.Add(new ArgInfo(name)));

        public static IArgBinder<T> Optional<T>(string name, T @default, IParser<T> parser) =>
            ArgBinder.Create(source => source.Lookup(name, null, null) is string s ? parser.Parse(s) : @default,
                             args => args.Add(new ArgInfo(name)));

        public static IArgBinder<T> Optional<T>(string name, IParser<T> parser) where T : class =>
            ArgBinder.Create(source => source.Lookup(name, null, null) is string s ? parser.Parse(s) : null,
                             args => args.Add(new ArgInfo(name)));

        public static IArgBinder<T?> OptionalValue<T>(string name, IParser<T> parser) where T : struct =>
            ArgBinder.Create(source => source.Lookup(name, null, null) is string s ? parser.Parse(s) : (T?)null,
                             args => args.Add(new ArgInfo(name)));
    }

    partial interface IArgSource
    {
        string Lookup(string longName, string shortName, string otherName);
    }

    partial class ArgSource : IArgSource
    {
        public static readonly IArgSource Empty = new EmptyArgSource();

        sealed class EmptyArgSource : IArgSource
        {
            public string Lookup(string longName, string shortName, string otherName) => null;
        }

        readonly (bool Taken, string Text)[] _args;

        public ArgSource(ICollection<string> args)
        {
            _args = new (bool Taken, string Text)[args.Count + 1];
            var a = _args.AsSpan(1);
            var i = 0;
            foreach (var arg in args)
                a[i++].Text = arg;
        }

        static readonly Func<string, bool> Mismatch = _ => false;

        public string Lookup(string longName, string shortName, string otherName)
        {
            return Lookup("--".PrependToSome(longName ) is string ln ? s => s == ln : Mismatch,
                          "--".PrependToSome(shortName) is string sn ? s => s == sn : Mismatch,
                          "--".PrependToSome(otherName) is string on ? s => s == on : Mismatch);

            string Lookup(Func<string, bool> @long, Func<string, bool> @short, Func<string, bool> other)
            {
                ref var prev = ref _args[0];
                foreach (ref var arg in _args.AsSpan())
                {
                    if (!prev.Taken && (@long(prev.Text) || @short(prev.Text) || other(prev.Text)))
                    {
                        prev.Taken = true;
                        arg.Taken = true;
                        return arg.Text;
                    }
                    prev = arg;
                }

                return null;
            }
        }

        public IEnumerable<string> Unused =>
            from arg in _args where !arg.Taken select arg.Text;
    }

    partial interface IArgBinder<out T>
    {
        T Bind(IArgSource source);
        void Inspect(ICollection<ArgInfo> args);
    }

    static partial class ArgBinder
    {
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

        public static T Bind<T>(this IArgBinder<T> binder, params string[] args) =>
            binder.Bind(new ArgSource(args));

        public static IArgBinder<T> Create<T>(Func<IArgSource, T> binder, Action<ICollection<ArgInfo>> inspector) =>
            new DelegatingArgBinder<T>(binder, inspector);

        public static IArgBinder<T> PassThru<T, U>(this IArgBinder<U> inner, Func<IArgSource, T> binder) =>
            new DelegatingArgBinder<T>(binder, inner.Inspect);

        public static IArgBinder<U> Select<T, U>(this IArgBinder<T> binder, Func<T, U> f) =>
            binder.PassThru(args => f(binder.Bind(args)));

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
