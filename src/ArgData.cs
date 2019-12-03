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
    public interface IArgData
    {
        string Description { get; }
        IArgData WithDescription(string value);
    }

    public abstract class ArgData : IArgData
    {
        protected ArgData(string description) =>
            Description = description;

        public string Description { get; }

        public ArgData WithDescription(string value) =>
            Update(value);

        IArgData IArgData.WithDescription(string value) =>
            WithDescription(value);

        protected abstract ArgData Update(string description);
    }

    public class OperandArgData : ArgData
    {
        public OperandArgData(string valueName) : this(valueName, null) {}

        public OperandArgData(string valueName, string description) :
            base(description) =>
            ValueName = valueName;

        public string ValueName { get; }

        public OperandArgData WithValueName(string value) =>
            Update(value, Description);

        public new OperandArgData WithDescription(string value) =>
            Update(ValueName, value);

        protected override ArgData Update(string description) =>
            Update(ValueName, description);

        protected virtual OperandArgData Update(string valueName, string description) =>
            new OperandArgData(valueName, description);
    }

    public class LiteralArgData : ArgData
    {
        public LiteralArgData(string text) : this(text, null) { }

        public LiteralArgData(string text, string description) :
            base(description) =>
            Text = text;

        public string Text { get; }

        public LiteralArgData WithText(string value) =>
            Update(value, Description);

        public new LiteralArgData WithDescription(string value) =>
            Update(Text, value);

        protected override ArgData Update(string description) =>
            Update(Text, description);

        protected virtual LiteralArgData Update(string text, string description) =>
            new LiteralArgData(text, description);
    }

    public enum OptionKind
    {
        Standard,
        Flag,
        Boolean,
    }

    public class OptionArgData : ArgData
    {
        public const string DefaultValueName = "VALUE";

        public OptionArgData(string name, ShortOptionName shortName) :
            this(OptionKind.Standard, name, shortName) {}

        public OptionArgData(OptionKind kind, string name, ShortOptionName shortName) :
            this(kind, name, shortName, null) {}

        public OptionArgData(OptionKind kind, string name, ShortOptionName shortName, string valueName) :
            this(kind, name, shortName, valueName, null) {}

        public OptionArgData(OptionKind kind, string name, ShortOptionName shortName, string valueName, string description) :
            base(description)
        {
            Kind = kind;
            Name = name;
            ShortName = shortName;
            ValueName = valueName ?? DefaultValueName;
        }

        public OptionKind Kind { get; }
        public string Name { get; }
        public ShortOptionName ShortName { get; }
        public string ValueName { get; }

        public OptionArgData WithName(string value) =>
            Update(value, ShortName, ValueName, Description);

        public OptionArgData WithShortName(ShortOptionName value) =>
            Update(Name, value, ValueName, Description);

        public OptionArgData WithValueName(string value) =>
            Update(value, ShortName, value, Description);

        public new OptionArgData WithDescription(string value) =>
            Update(Name, ShortName, ValueName, value);

        protected override ArgData Update(string description) =>
            Update(Name, ShortName, ValueName, description);

        protected virtual OptionArgData Update(string name, ShortOptionName shortName, string valueName, string description) =>
            new OptionArgData(Kind, name, shortName, valueName, description);
    }

    public class IntegerOptionArgData : ArgData
    {
        public IntegerOptionArgData(string valueName) : this(valueName, null) { }

        public IntegerOptionArgData(string valueName, string description) :
            base(description) =>
            ValueName = valueName;

        public string ValueName { get; }

        public IntegerOptionArgData WithValueName(string value) =>
            Update(value, Description);

        public new IntegerOptionArgData WithDescription(string value) =>
            Update(ValueName, value);

        protected override ArgData Update(string description) =>
            Update(ValueName, description);

        protected virtual IntegerOptionArgData Update(string valueName, string description) =>
            new IntegerOptionArgData(valueName, description);
    }
}
