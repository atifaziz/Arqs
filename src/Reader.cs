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

    public sealed class Reader<T> : IDisposable
    {
        (bool, T) _next;
        Stack<T> _nextItems;
        IEnumerator<T> _enumerator;

        public Reader(IEnumerable<T> items) =>
            _enumerator = items.GetEnumerator();

        public void Dispose()
        {
            var args  = _enumerator;
            _enumerator = null;
            args?.Dispose();
        }

        public bool HasMore() => TryPeek(out _);

        public bool TryPeek(out T item)
        {
            if (!TryRead(out item))
                return false;
            Unread(item);
            return true;
        }

        internal void Unread(T item)
        {
            var (hasNext, next) = _next;
            if (hasNext)
            {
                _next = default;
                _nextItems = new Stack<T>();
                _nextItems.Push(next);
            }
            else if (_nextItems == null)
            {
                _next = (true, item);
                return;
            }

            _nextItems.Push(item);
        }

        public T Read() =>
            TryRead(out var item) ? item : throw new InvalidOperationException();

        public bool TryRead(out T item)
        {
            var (hasNext, next) = _next;

            if (hasNext)
            {
                item = next;
                _next = default;
                return true;
            }

            if (_nextItems?.Count > 0)
            {
                item = _nextItems.Pop();
                return true;
            }

            if (_enumerator == null)
            {
                item = default;
                return false;
            }

            if (!_enumerator.MoveNext())
            {
                _enumerator.Dispose();
                _enumerator = null;
                item = default;
                return false;
            }

            item = _enumerator.Current;
            return true;
        }
    }
}
