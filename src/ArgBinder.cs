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

    public interface IArgBinder
    {
        object Bind(Func<IArg, IAccumulator> source);
        IEnumerable<IArg> Inspect();
    }

    public interface IArgBinder<out T> : IArgBinder
    {
        new T Bind(Func<IArg, IAccumulator> source);
    }

    public static class ArgBinder
    {
        public static IList<IArg> Inspect<T>(this IArgBinder<T> binder) =>
            binder.Inspect().ToList();

        public static IArgBinder<(T, U)> Zip<T, U>(this IArgBinder<T> first, IArgBinder<U> second) =>
            Create(bindings => (first.Bind(bindings), second.Bind(bindings)),
                   () => first.Inspect().Concat(second.Inspect()));

        enum BindMode { Strict, Tolerant }

        public static (T Result, ImmutableArray<string> Tail)
            Bind<T>(this IArgBinder<T> binder, params string[] args) =>
            binder.Bind(BindMode.Strict, args);

        public static (M Mode, T Result, ImmutableArray<string> Tail)
            Bind<T, M>(IArgBinder<(bool, M)> modalBinder1,
                       IArgBinder<(bool, M)> modalBinder2,
                       IArgBinder<(M, T)> binder,
                       params string[] args) =>
            Bind(new[] { modalBinder1, modalBinder2 }, binder, args);

        public static (M Mode, T Result, ImmutableArray<string> Tail)
            Bind<T, M>(IEnumerable<IArgBinder<(bool, M)>> modalBinders,
                       IArgBinder<(M, T)> binder,
                       params string[] args)
        {
            ImmutableArray<string> tail;
            foreach (var modalBinder in modalBinders)
            {
                var ((flag, mode), tail2) = modalBinder.Bind(BindMode.Tolerant, args);
                if (flag)
                    return (mode, default, tail2);
                tail = tail2;
            }
            var ((mode3, result), tail3) = binder.Bind(tail.ToArray());
            return (mode3, result, tail3);
        }

        static (T Result, ImmutableArray<string> Tail)
            Bind<T>(this IArgBinder<T> binder, BindMode mode, params string[] args)
        {
            var specs = binder.Inspect().ToList();

            var accumulators = new IAccumulator[specs.Count];
            for (var i = 0; i < specs.Count; i++)
                accumulators[i] = specs[i].CreateAccumulator();

            var asi = 0;
            var tail = new List<string>();

            using var reader = args.Read();
            while (reader.TryPeek(out var arg))
            {
                string name = null;
                if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    name = arg.Substring(2);
                }
                else if (arg.Length > 1 && arg[0] == '-')
                {
                    if (arg.Length > 2)
                    {
                        reader.Read();
                        foreach (var ch in arg.Substring(1).Reverse())
                        {
                            var i = specs.FindIndex(e => e.Name() is string s && s.Length == 1 && s[0] == ch);
                            if (i >= 0)
                            {
                                reader.Unread("-" + ch);
                            }
                            else
                            {
                                if (mode == BindMode.Strict)
                                    throw new Exception("Invalid option: " + ch);
                                tail.Add("-" + ch);
                            }
                        }
                        continue;
                    }

                    name = arg.Substring(1, 1);
                }

                if (name != null)
                {
                    var i = specs.FindIndex(e => e.Name() == name);
                    if (i >= 0)
                    {
                        reader.Read();
                        if (specs[i].IsFlag())
                            reader.Unread("+");
                        if (!accumulators[i].Read(reader))
                            throw new Exception("Invalid value for option: " + name);
                    }
                    else
                    {
                        if (mode == BindMode.Strict)
                            throw new Exception("Invalid option: " + name);
                        reader.Read();
                        tail.Add(arg);
                    }
                }
                else
                {
                    var i = specs.FindIndex(asi, e => e.Name() == null);
                    if (i >= 0)
                    {
                        asi = i + 1;
                        if (!accumulators[i].Read(reader))
                            throw new Exception("Invalid argument: " + arg);
                    }
                    else
                    {
                        reader.Read();
                        tail.Add(arg);
                    }
                }
            }

            return (binder.Bind(info => accumulators[specs.IndexOf(info)]), tail.ToImmutableArray());
        }

        public static IArgBinder<T> Create<T>(Func<Func<IArg, IAccumulator>, T> binder, Func<IEnumerable<IArg>> inspector) =>
            new DelegatingArgBinder<T>(binder, inspector);

        public static IArgBinder<U> Select<T, U>(this IArgBinder<T> binder, Func<T, U> f) =>
            Create(bindings => f(binder.Bind(bindings)), binder.Inspect);

        public static IArgBinder<U> SelectMany<T, U>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f) =>
            Create(bindings => f(binder.Bind(bindings)).Bind(bindings),
                   () => binder.Inspect().Concat(f(binder.Bind(delegate { throw new InvalidOperationException(); })).Inspect()));

        public static IArgBinder<V> SelectMany<T, U, V>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f, Func<T, U, V> g) =>
            binder.Select(t => f(t).Select(u => g(t, u))).SelectMany(pv => pv);

        public static IArgBinder<V> Join<T, U, K, V>(this IArgBinder<T> first, IArgBinder<U> second,
            Func<T, K> unused1, Func<T, K> unused2,
            Func<T, U, V> resultSelector) =>
            from ab in first.Zip(second)
            select resultSelector(ab.Item1, ab.Item2);

        sealed class DelegatingArgBinder<T> : IArgBinder<T>
        {
            readonly Func<Func<IArg, IAccumulator>, T> _binder;
            readonly Func<IEnumerable<IArg>> _inspector;

            public DelegatingArgBinder(Func<Func<IArg, IAccumulator>, T> binder,
                                       Func<IEnumerable<IArg>> inspector)
            {
                _binder = binder;
                _inspector = inspector;
            }

            object IArgBinder.Bind(Func<IArg, IAccumulator> source) =>
                Bind(source);

            public T Bind(Func<IArg, IAccumulator> source) =>
                _binder(source);

            public IEnumerable<IArg> Inspect() =>
                _inspector();
        }
    }
}
