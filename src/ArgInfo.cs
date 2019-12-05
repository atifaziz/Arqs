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

    public class MacroArgInfo : ArgInfo
    {
        public MacroArgInfo(string valueName) : this(valueName, null) { }

        public MacroArgInfo(string valueName, string description) :
            base(description) =>
            ValueName = valueName;

        public string ValueName { get; }

        public MacroArgInfo WithValueName(string value) =>
            Update(value, Description);

        public new MacroArgInfo WithDescription(string value) =>
            Update(ValueName, value);

        protected override ArgInfo Update(string description) =>
            Update(ValueName, description);

        protected virtual MacroArgInfo Update(string valueName, string description) =>
            new MacroArgInfo(valueName, description);
    }

    public class OptionArgInfo : ArgInfo
    {
        public const string DefaultValueName = "VALUE";

        public OptionArgInfo(string name, ShortOptionName shortName) :
            this(name, shortName, null) {}

        public OptionArgInfo(string name, ShortOptionName shortName, string valueName) :
            this(name, shortName, valueName, null) {}

        public OptionArgInfo(string name, ShortOptionName shortName, string valueName, string description) :
            this(name, shortName, valueName, false, description) {}

        public OptionArgInfo(string name, ShortOptionName shortName, string valueName, bool isValueOptional, string description) :
            base(description)
        {
            Name = name;
            ShortName = shortName;
            IsValueOptional = isValueOptional;
            ValueName = valueName ?? DefaultValueName;
        }

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
            new OptionArgInfo(name, shortName, valueName, isValueOptional, description);
    }

    public class FlagArgInfo : ArgInfo
    {
        public FlagArgInfo(string name, ShortOptionName shortName) :
            this(name, shortName, false, null) {}

        public FlagArgInfo(string name, ShortOptionName shortName, bool isNegatable, string description) :
            base(description)
        {
            Name = name;
            ShortName = shortName;
            IsNegatable = isNegatable;
        }

        public string Name { get; }
        public ShortOptionName ShortName { get; }
        public bool IsNegatable { get; }

        public FlagArgInfo WithName(string value) =>
            Update(value, ShortName, IsNegatable, Description);

        public FlagArgInfo WithShortName(ShortOptionName value) =>
            Update(Name, value, IsNegatable, Description);

        public FlagArgInfo WithIsNegatable(bool value) =>
            Update(Name, ShortName, value, Description);

        public new FlagArgInfo WithDescription(string value) =>
            Update(Name, ShortName, IsNegatable, value);

        protected override ArgInfo Update(string description) =>
            Update(Name, ShortName, IsNegatable, description);

        protected virtual FlagArgInfo Update(string name, ShortOptionName shortName, bool isNegatable, string description) =>
            new FlagArgInfo(name, shortName, isNegatable, description);
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
