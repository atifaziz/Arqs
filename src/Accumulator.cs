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

    public interface IAccumulator
    {
        int Count { get; }
        object GetResult();
        bool Accumulate(Reader<string> arg);
        void AccumulateDefault();
    }

    public interface IAccumulator<out T> : IAccumulator
    {
        new T GetResult();
    }

    public static class Accumulator
    {
        public static IAccumulator<T> Value<T>(IParser<T> parser) =>
            Value(parser, default, (_, v) => v);

        static readonly IAccumulator<int> Counter =
            Create(0, (count, _) => ParseResult.Success(count + 1), count => count);

        public static IAccumulator<int> Count() => Counter;

        public static IAccumulator<T> Value<T>(IParser<T> parser, T seed, Func<T, T, T> folder) =>
            Create(seed, (acc, arg) => arg.TryRead(out var s) && parser.Parse(s) is (true, var v)
                                     ? ParseResult.Success(folder(acc, v)) : default,
                         v => v);

        public static IAccumulator<T> Value<T>(IParser<T> parser, T seed, T @default, Func<T, T, T> folder) =>
            Create(seed, (acc, arg) => arg.TryRead(out var s) && parser.Parse(s) is (true, var v)
                                     ? ParseResult.Success(folder(acc, v)) : default,
                         s => folder(s, @default),
                         v => v);

        public static IAccumulator<T> Return<T>(T value) =>
            new DelegatingAccumulator<T, T>(true, value, (v, arg) => ParseResult.Success(v), r => r);

        public static IAccumulator<T> Create<T>(T seed, Func<T, Reader<string>, ParseResult<T>> reader) =>
            new DelegatingAccumulator<T, T>(seed, reader, v => v);

        public static IAccumulator<R> Create<T, R>(T seed, Func<T, Reader<string>, ParseResult<T>> reader, Func<T, R> resultSelector) =>
            new DelegatingAccumulator<T, R>(seed, reader, resultSelector);

        public static IAccumulator<R> Create<T, R>(T seed, Func<T, Reader<string>, ParseResult<T>> reader, Func<T, T> defaultor, Func<T, R> resultSelector) =>
            new DelegatingAccumulator<T, R>(seed, reader, defaultor, resultSelector);

        public static IAccumulator<U> Select<T, U>(this IAccumulator<T> accumulator, Func<T, U> f) =>
            Create(0, (count, arg) => accumulator.Accumulate(arg) ? ParseResult.Success(count + 1) : default,
                   r => f(accumulator.GetResult()));

        sealed class DelegatingAccumulator<S, R> : IAccumulator<R>
        {
            readonly Func<S, Reader<string>, ParseResult<S>> _reader;
            readonly Func<S, S> _defaultor;
            readonly Func<S, R> _resultSelector;
            S _state;
            bool _errored;

            public DelegatingAccumulator(bool initialized, S seed,
                                         Func<S, Reader<string>, ParseResult<S>> reader,
                                         Func<S, R> resultSelector) :
                this(initialized, seed, reader, null, resultSelector) {}

            public DelegatingAccumulator(S seed,
                                         Func<S, Reader<string>, ParseResult<S>> reader,
                                         Func<S, R> resultSelector) :
                this(seed, reader, null, resultSelector) {}

            public DelegatingAccumulator(S seed,
                                         Func<S, Reader<string>, ParseResult<S>> reader,
                                         Func<S, S> defaultor,
                                         Func<S, R> resultSelector) :
                this(false, seed, reader, defaultor, resultSelector) {}

            public DelegatingAccumulator(bool initialized, S seed,
                                         Func<S, Reader<string>, ParseResult<S>> reader,
                                         Func<S, S> defaultor,
                                         Func<S, R> resultSelector)
            {
                Count = initialized ? 1 : 0;
                _state = seed;
                _reader = reader ?? throw new ArgumentNullException(nameof(reader));
                _defaultor = defaultor;
                _resultSelector = resultSelector;
            }

            public int Count { get; private set; }

            public R GetResult() => !_errored && Count > 0 ? _resultSelector(_state)
                                                           : throw new InvalidOperationException();

            object IAccumulator.GetResult() => GetResult();

            public bool Accumulate(Reader<string> arg)
            {
                if (_errored)
                    throw new InvalidOperationException();

                switch (_reader(_state, arg))
                {
                    case (true, var value):
                        Count++;
                        _state = value;
                        return true;
                    default:
                        _errored = true;
                        _state = default;
                        return false;
                }
            }

            public void AccumulateDefault()
            {
                if (_errored)
                    throw new InvalidOperationException();
                if (_defaultor == null)
                    throw new InvalidOperationException();
                Count++;
                _state = _defaultor(_state);
            }
        }
    }
}
