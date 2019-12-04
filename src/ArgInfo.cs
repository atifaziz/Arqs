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
    public interface IArgInfo
    {
        string Description { get; }
        IArgInfo WithDescription(string value);
    }

    public abstract class ArgInfo : IArgInfo
    {
        protected ArgInfo(string description) =>
            Description = description;

        public string Description { get; }

        public ArgInfo WithDescription(string value) =>
            Update(value);

        IArgInfo IArgInfo.WithDescription(string value) =>
            WithDescription(value);

        protected abstract ArgInfo Update(string description);
    }

    public class OperandArgInfo : ArgInfo
    {
        public OperandArgInfo(string valueName) : this(valueName, null) {}

        public OperandArgInfo(string valueName, string description) :
            base(description) =>
            ValueName = valueName;

        public string ValueName { get; }

        public OperandArgInfo WithValueName(string value) =>
            Update(value, Description);

        public new OperandArgInfo WithDescription(string value) =>
            Update(ValueName, value);

        protected override ArgInfo Update(string description) =>
            Update(ValueName, description);

        protected virtual OperandArgInfo Update(string valueName, string description) =>
            new OperandArgInfo(valueName, description);
    }

    public class LiteralArgInfo : ArgInfo
    {
        public LiteralArgInfo(string text) : this(text, null) { }

        public LiteralArgInfo(string text, string description) :
            base(description) =>
            Text = text;

        public string Text { get; }

        public LiteralArgInfo WithText(string value) =>
            Update(value, Description);

        public new LiteralArgInfo WithDescription(string value) =>
            Update(Text, value);

        protected override ArgInfo Update(string description) =>
            Update(Text, description);

        protected virtual LiteralArgInfo Update(string text, string description) =>
            new LiteralArgInfo(text, description);
    }

    public enum OptionArgKind
    {
        Standard,
        Flag,
        Boolean,
    }

    public class OptionArgInfo : ArgInfo
    {
        public const string DefaultValueName = "VALUE";

        public OptionArgInfo(string name, ShortOptionName shortName) :
            this(OptionArgKind.Standard, name, shortName) {}

        public OptionArgInfo(OptionArgKind argKind, string name, ShortOptionName shortName) :
            this(argKind, name, shortName, null) {}

        public OptionArgInfo(OptionArgKind argKind, string name, ShortOptionName shortName, string valueName) :
            this(argKind, name, shortName, valueName, null) {}

        public OptionArgInfo(OptionArgKind argKind, string name, ShortOptionName shortName, string valueName, string description) :
            this(argKind, name, shortName, valueName, false, description) {}

        public OptionArgInfo(OptionArgKind argKind, string name, ShortOptionName shortName, string valueName, bool isValueOptional, string description) :
            base(description)
        {
            ArgKind = argKind;
            Name = name;
            ShortName = shortName;
            IsValueOptional = isValueOptional;
            ValueName = valueName ?? DefaultValueName;
        }

        public OptionArgKind ArgKind { get; }
        public string Name { get; }
        public ShortOptionName ShortName { get; }
        public bool IsValueOptional { get; }
        public string ValueName { get; }

        public OptionArgInfo WithName(string value) =>
            Update(value, ShortName, IsValueOptional, ValueName, Description);

        public OptionArgInfo WithShortName(ShortOptionName value) =>
            Update(Name, value, IsValueOptional, ValueName, Description);

        public OptionArgInfo WithIsValueOptional(bool value) =>
            Update(Name, ShortName, value, ValueName, Description);

        public OptionArgInfo WithValueName(string value) =>
            Update(value, ShortName, IsValueOptional, value, Description);

        public new OptionArgInfo WithDescription(string value) =>
            Update(Name, ShortName, IsValueOptional, ValueName, value);

        protected override ArgInfo Update(string description) =>
            Update(Name, ShortName, IsValueOptional, ValueName, description);

        protected virtual OptionArgInfo Update(string name, ShortOptionName shortName, bool isValueOptional, string valueName, string description) =>
            new OptionArgInfo(ArgKind, name, shortName, valueName, isValueOptional, description);
    }

    public class IntegerOptionArgInfo : ArgInfo
    {
        public IntegerOptionArgInfo(string valueName) : this(valueName, null) { }

        public IntegerOptionArgInfo(string valueName, string description) :
            base(description) =>
            ValueName = valueName;

        public string ValueName { get; }

        public IntegerOptionArgInfo WithValueName(string value) =>
            Update(value, Description);

        public new IntegerOptionArgInfo WithDescription(string value) =>
            Update(ValueName, value);

        protected override ArgInfo Update(string description) =>
            Update(ValueName, description);

        protected virtual IntegerOptionArgInfo Update(string valueName, string description) =>
            new IntegerOptionArgInfo(valueName, description);
    }
}
