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
        bool HasValue { get; }
        object Value { get; }
        bool Read(Reader<string> arg);
    }

    public interface IAccumulator<out T> : IAccumulator
    {
        new T Value { get; }
    }

    public static class Accumulator
    {
        public static IAccumulator<T> Value<T>(IParser<T> parser) =>
            new ValueAccumulator<T>(default, (_, arg) => arg.TryRead(out var v) ? parser.Parse(v) : default);

        public static IAccumulator<int> Flag() =>
            new ValueAccumulator<int>(0, (count, _) => ParseResult.Success(count + 1));

        sealed class ValueAccumulator<T> : IAccumulator<T>
        {
            public bool HasValue { get; private set; }
            public T Value { get; private set; }

            object IAccumulator.Value => Value;

            readonly Func<T, Reader<string>, ParseResult<T>> _reader;

            public ValueAccumulator(T initial, Func<T, Reader<string>, ParseResult<T>> reader)
            {
                Value = initial;
                _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            }

            public bool Read(Reader<string> arg)
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
}
