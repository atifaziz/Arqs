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

    public interface ICli
    {
        object Bind(Func<IAccumulator> source);
        IEnumerable<ICliRecord> Inspect();
    }

    public interface ICli<out T> : ICli
    {
        new T Bind(Func<IAccumulator> source);
    }

    public static class Cli
    {
        public static ICli<(T, U)> Zip<T, U>(this ICli<T> first, ICli<U> second) =>
            Create(bindings => (first.Bind(bindings), second.Bind(bindings)),
                   () => first.Inspect().Concat(second.Inspect()));

        public static ICli<T> Create<T>(Func<Func<IAccumulator>, T> binder, Func<IEnumerable<ICliRecord>> inspector) =>
            new DelegatingCli<T>(binder, inspector);

        public static ICli<T> Return<T>(T value) =>
            Create(_ => value, Enumerable.Empty<ICliRecord>);

        public static ICli<U> Select<T, U>(this ICli<T> cli, Func<T, U> f) =>
            Create(bindings => f(cli.Bind(bindings)), cli.Inspect);

        public static ICli<V> Join<T, U, K, V>(this ICli<T> first, ICli<U> second,
            Func<T, K> unused1, Func<T, K> unused2,
            Func<T, U, V> resultSelector) =>
            from ab in first.Zip(second)
            select resultSelector(ab.Item1, ab.Item2);

        sealed class DelegatingCli<T> : ICli<T>
        {
            readonly Func<Func<IAccumulator>, T> _binder;
            readonly Func<IEnumerable<ICliRecord>> _inspector;

            public DelegatingCli(Func<Func<IAccumulator>, T> binder,
                                 Func<IEnumerable<ICliRecord>> inspector)
            {
                _binder = binder;
                _inspector = inspector;
            }

            object ICli.Bind(Func<IAccumulator> source) =>
                Bind(source);

            public T Bind(Func<IAccumulator> source) =>
                _binder(source);

            public IEnumerable<ICliRecord> Inspect() =>
                _inspector();
        }
    }
}
