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

    partial interface IArgSource
    {
        string Lookup(string longName, string shortName, string otherName);
    }

    partial class ArgSource : IArgSource
    {
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
    }

    static partial class ArgBinder
    {
        public static T Bind<T>(this IArgBinder<T> binder, params string[] args) =>
            binder.Bind(new ArgSource(args));

        public static IArgBinder<T> Create<T>(Func<IArgSource, T> binder) =>
            new DelegatingArgBinder<T>(binder);

        public static IArgBinder<U> Select<T, U>(this IArgBinder<T> binder, Func<T, U> f) =>
            Create(args => f(binder.Bind(args)));

        public static IArgBinder<U> SelectMany<T, U>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f) =>
            Create(args => f(binder.Bind(args)).Bind(args));

        public static IArgBinder<V> SelectMany<T, U, V>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f, Func<T, U, V> g) =>
            binder.Select(t => f(t).Select(u => g(t, u))).SelectMany(pv => pv);

        sealed class DelegatingArgBinder<T> : IArgBinder<T>
        {
            readonly Func<IArgSource, T> _binder;

            public DelegatingArgBinder(Func<IArgSource, T> binder) =>
                _binder = binder;

            public T Bind(IArgSource source) =>
                _binder(source);
        }
    }
}
