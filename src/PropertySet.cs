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

    public sealed class Property : IEquatable<Property>
    {
        enum Flags
        {
            None,
            Writable,
        }

        public static Property ReadOnly(Symbol symbol) => new Property(symbol, Flags.None);
        public static Property Writable(Symbol symbol) => new Property(symbol, Flags.Writable);

        readonly Symbol _symbol;
        readonly Flags _flags;

        Property(Symbol symbol, Flags flags)
        {
            _symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            _flags = flags;
        }

        public bool IsWritable => _flags.HasFlag(Flags.Writable);

        public override string ToString() => _symbol.ToString();

        public bool Equals(Property other) =>
            !ReferenceEquals(null, other) &&
            (ReferenceEquals(this, other) || Equals(_symbol, other._symbol));

        public override bool Equals(object obj) =>
            obj is Property other && Equals(other);

        public override int GetHashCode() =>
            _symbol.GetHashCode();
    }

    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public sealed class PropertySet
    {
        public static readonly PropertySet Empty = new PropertySet(ImmutableDictionary<Property, object>.Empty);

        readonly ImmutableDictionary<Property, object> _properties;

        public PropertySet(Property property, object value) :
            this(ImmutableDictionary<Property, object>.Empty.Add(property, value)) {}

        public PropertySet(ImmutableDictionary<Property, object> properties) =>
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));

        public int Count => _properties.Count;

        public object this[Property key] => _properties.TryGetValue(key, out var value) ? value : null;

        public PropertySet With(Property key, object value) =>
            key.IsWritable
            ? this[key] == value ? this : new PropertySet(_properties.Add(key, value))
            : throw new InvalidOperationException();
    }
}
