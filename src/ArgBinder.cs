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

namespace Arqs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public interface IArgBinder
    {
        object Bind(Func<IAccumulator> source);
        IEnumerable<IInspectionRecord> Inspect();
    }

    public interface IArgBinder<out T> : IArgBinder
    {
        new T Bind(Func<IAccumulator> source);
    }

    public static class ArgBinder
    {
        public static IArgBinder<(T, U)> Zip<T, U>(this IArgBinder<T> first, IArgBinder<U> second) =>
            Create(bindings => (first.Bind(bindings), second.Bind(bindings)),
                   () => first.Inspect().Concat(second.Inspect()));

        public static IArgBinder<T> Create<T>(Func<Func<IAccumulator>, T> binder, Func<IEnumerable<IInspectionRecord>> inspector) =>
            new DelegatingArgBinder<T>(binder, inspector);

        public static IArgBinder<U> Select<T, U>(this IArgBinder<T> binder, Func<T, U> f) =>
            Create(bindings => f(binder.Bind(bindings)), binder.Inspect);

        public static IArgBinder<U> SelectMany<T, U>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f) =>
            Create(bindings => f(binder.Bind(bindings)).Bind(bindings),
                   () => binder.Inspect().Concat(f(default).Inspect()));

        public static IArgBinder<V> SelectMany<T, U, V>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f, Func<T, U, V> g) =>
            Create(bindings =>
                {
                    var a = binder.Bind(bindings);
                    return g(a, f(a).Bind(bindings));
                },
                () =>
                {
                    var a = binder.Inspect();
                    return a.Concat(f(default).Inspect());
                });

        public static IArgBinder<V> Join<T, U, K, V>(this IArgBinder<T> first, IArgBinder<U> second,
            Func<T, K> unused1, Func<T, K> unused2,
            Func<T, U, V> resultSelector) =>
            from ab in first.Zip(second)
            select resultSelector(ab.Item1, ab.Item2);

        sealed class DelegatingArgBinder<T> : IArgBinder<T>
        {
            readonly Func<Func<IAccumulator>, T> _binder;
            readonly Func<IEnumerable<IInspectionRecord>> _inspector;

            public DelegatingArgBinder(Func<Func<IAccumulator>, T> binder,
                                       Func<IEnumerable<IInspectionRecord>> inspector)
            {
                _binder = binder;
                _inspector = inspector;
            }

            object IArgBinder.Bind(Func<IAccumulator> source) =>
                Bind(source);

            public T Bind(Func<IAccumulator> source) =>
                _binder(source);

            public IEnumerable<IInspectionRecord> Inspect() =>
                _inspector();
        }
    }
}
