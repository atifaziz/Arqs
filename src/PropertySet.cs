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
    using System.Collections.Immutable;
    using System.Diagnostics;

    public sealed class Symbol
    {
        public static Symbol New() => New(null);
        public static Symbol New(string description) => new Symbol(description);

        readonly string _description;

        Symbol(string description) => _description = description;

        public override string ToString() => _description ?? "#" + GetHashCode();
    }

    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public sealed class PropertySet
    {
        public static readonly PropertySet Empty = new PropertySet(ImmutableDictionary<Symbol, object>.Empty);

        readonly ImmutableDictionary<Symbol, object> _properties;

        public PropertySet(Symbol property, object value) :
            this(ImmutableDictionary<Symbol, object>.Empty.Add(property, value)) {}

        public PropertySet(ImmutableDictionary<Symbol, object> properties) =>
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));

        public int Count => _properties.Count;

        public object this[Symbol key] => _properties.TryGetValue(key, out var value) ? value : null;

        public PropertySet Set(Symbol key, object value) =>
            this[key] == value ? this : new PropertySet(_properties.SetItem(key, value));
    }
}
