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

// ReSharper disable CheckNamespace

using System;

static partial class ArraySpanExtensions
{
    public static ArraySpan<T> AsSpan<T>(this T[] array) =>
        AsSpan(array, 0, array.Length);

    public static ArraySpan<T> AsSpan<T>(this T[] array, int index) =>
        AsSpan(array, index, array.Length - index);

    public static ArraySpan<T> AsSpan<T>(this T[] array, int index, int length) =>
        new ArraySpan<T>(array, index, length);
}

readonly partial struct ArraySpan<T>
{
    readonly T[] _array;
    readonly int _index;
    readonly int _length;

    public ArraySpan(T[] array) :
        this(array, 0, array.Length) {}

    public ArraySpan(T[] array, int index) :
        this(array, index, array.Length - index) {}

    public ArraySpan(T[] array, int index, int length)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        ValidateSpan(index, length, array.Length);

        _array  = array;
        _index  = index;
        _length = length;
    }

    static void ValidateSpan(int index, int length, int actualLength)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (index + length > actualLength) throw new ArgumentOutOfRangeException(nameof(index));
    }

    public int Length => Math.Max(_length, 0);

    public ArraySpan<T> Slice(int index) =>
        index == 0 ? this : Slice(index, Length - index);

    public ArraySpan<T> Slice(int index, int length)
    {
        ValidateSpan(index, length, Length);
        return new ArraySpan<T>(_array, _index + index, length);
    }

    public ref T this[int index] => ref _array[_index + index];

    public Enumerator GetEnumerator() =>
        new Enumerator(_array, _index, Length);

    public ref struct Enumerator
    {
        readonly T[] _array;
        readonly int _length;
        int _index;

        public Enumerator(T[] array) :
            this(array, 0, array.Length) {}

        public Enumerator(T[] array, int index) :
            this(array, index, array.Length - index) {}

        public Enumerator(T[] array, int index, int length)
        {
            ValidateSpan(index, length, array.Length);

            _array  = array;
            _index  = index - 1;
            _length = length;
        }

        public bool MoveNext()
        {
            var index = _index + 1;
            if (index >= _length)
                return false;
            _index = index;
            return true;
        }

        public ref T Current => ref _array[_index];
    }
}
