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

    public enum Visibility
    {
        Default,
        Hidden,
    }

    public interface IArgInfo
    {
        string Description { get; }
        Visibility Visibility { get; }
        IArgInfo WithDescription(string value);
        IArgInfo WithVisibility(Visibility value);
    }

    public abstract class ArgInfo : IArgInfo
    {
        protected ArgInfo(string description, Visibility visibility)
        {
            Description = description;
            Visibility = visibility;
        }

        public string Description { get; }
        public Visibility Visibility { get; }

        public ArgInfo WithDescription(string value) =>
            Update(value, Visibility);

        public ArgInfo WithVisibility(Visibility value) =>
            Update(Description, value);

        IArgInfo IArgInfo.WithVisibility(Visibility value) =>
            WithVisibility(value);

        IArgInfo IArgInfo.WithDescription(string value) =>
            WithDescription(value);

        protected abstract ArgInfo Update(string description, Visibility visibility);
    }

    public class OperandArgInfo : ArgInfo
    {
        public OperandArgInfo(string valueName) :
            this(valueName, null) {}

        public OperandArgInfo(string valueName, string description) :
            this(valueName, description, Visibility.Default) {}

        public OperandArgInfo(string valueName, string description, Visibility visibility) :
            base(description, visibility) =>
            ValueName = valueName;

        public string ValueName { get; }

        public OperandArgInfo WithValueName(string value) =>
            Update(value, Description, Visibility);

        public new OperandArgInfo WithDescription(string value) =>
            Update(ValueName, value, Visibility);

        public new OperandArgInfo WithVisibility(Visibility value) =>
            Update(ValueName, Description, value);

        protected override ArgInfo Update(string description, Visibility visibility) =>
            Update(ValueName, description, visibility);

        protected virtual OperandArgInfo Update(string valueName, string description, Visibility visibility) =>
            new OperandArgInfo(valueName, description, visibility);
    }

    public class MacroArgInfo : ArgInfo
    {
        public MacroArgInfo(string valueName) :
            this(valueName, null) {}

        public MacroArgInfo(string valueName, string description) :
            this(valueName, description, Visibility.Default) {}

        public MacroArgInfo(string valueName, string description, Visibility visibility) :
            base(description, visibility) =>
            ValueName = valueName;

        public string ValueName { get; }

        public MacroArgInfo WithValueName(string value) =>
            Update(value, Description, Visibility);

        public new MacroArgInfo WithDescription(string value) =>
            Update(ValueName, value, Visibility);

        public new MacroArgInfo WithVisibility(Visibility value) =>
            Update(ValueName, Description, value);

        protected override ArgInfo Update(string description, Visibility visibility) =>
            Update(ValueName, description, visibility);

        protected virtual MacroArgInfo Update(string valueName, string description, Visibility visibility) =>
            new MacroArgInfo(valueName, description, visibility);
    }

    public class OptionArgInfo : ArgInfo
    {
        public const string DefaultValueName = "VALUE";

        public OptionArgInfo(OptionNames names) :
            this(names, null) {}

        public OptionArgInfo(OptionNames names, string valueName) :
            this(names, valueName, null) {}

        public OptionArgInfo(OptionNames names, string valueName, string description) :
            this(names, valueName, false, description) {}

        public OptionArgInfo(OptionNames names, string valueName, bool isValueOptional, string description) :
            this(names, valueName, isValueOptional, description, Visibility.Default) {}

        public OptionArgInfo(OptionNames names, string valueName, bool isValueOptional,
                             string description, Visibility visibility) :
            base(description, visibility)
        {
            Names = names ?? throw new ArgumentNullException(nameof(names));
            IsValueOptional = isValueOptional;
            ValueName = valueName ?? DefaultValueName;
        }

        public OptionNames Names { get; }
        public bool IsValueOptional { get; }
        public string ValueName { get; }

        public OptionArgInfo WithNames(OptionNames value) =>
            Update(value, IsValueOptional, ValueName, Description, Visibility);

        public OptionArgInfo WithIsValueOptional(bool value) =>
            Update(Names, value, ValueName, Description, Visibility);

        public OptionArgInfo WithValueName(string value) =>
            Update(Names, IsValueOptional, value, Description, Visibility);

        public new OptionArgInfo WithDescription(string value) =>
            Update(Names, IsValueOptional, ValueName, value, Visibility);

        public new OptionArgInfo WithVisibility(Visibility value) =>
            Update(Names, IsValueOptional, ValueName, Description, Visibility);

        protected override ArgInfo Update(string description, Visibility visibility) =>
            Update(Names, IsValueOptional, ValueName, description, visibility);

        protected virtual OptionArgInfo Update(OptionNames names, bool isValueOptional, string valueName,
                                               string description, Visibility visibility) =>
            new OptionArgInfo(names, valueName, isValueOptional, description, visibility);
    }

    public class FlagArgInfo : ArgInfo
    {
        public FlagArgInfo(OptionNames names) :
            this(names, false, null) {}

        public FlagArgInfo(OptionNames names, bool isNegatable, string description) :
            this(names, isNegatable, description, Visibility.Default) {}

        public FlagArgInfo(OptionNames names, bool isNegatable, string description, Visibility visibility) :
            base(description, visibility)
        {
            Names = names ?? throw new ArgumentNullException(nameof(names));
            IsNegatable = isNegatable;
        }

        public OptionNames Names { get; }
        public bool IsNegatable { get; }

        public FlagArgInfo WithNames(OptionNames value) =>
            Update(value, IsNegatable, Description, Visibility);

        public FlagArgInfo WithIsNegatable(bool value) =>
            Update(Names, value, Description, Visibility);

        public new FlagArgInfo WithDescription(string value) =>
            Update(Names, IsNegatable, value, Visibility);

        public new FlagArgInfo WithVisibility(Visibility value) =>
            Update(Names, IsNegatable, Description, value);

        protected override ArgInfo Update(string description, Visibility visibility) =>
            Update(Names, IsNegatable, description, visibility);

        protected virtual FlagArgInfo Update(OptionNames names, bool isNegatable,
                                             string description, Visibility visibility) =>
            new FlagArgInfo(names, isNegatable, description, visibility);
    }

    public class IntegerOptionArgInfo : ArgInfo
    {
        public IntegerOptionArgInfo(string valueName) :
            this(valueName, null) {}

        public IntegerOptionArgInfo(string valueName, string description) :
            this(valueName, description, Visibility.Default) {}

        public IntegerOptionArgInfo(string valueName, string description, Visibility visibility) :
            base(description, visibility) =>
            ValueName = valueName;

        public string ValueName { get; }

        public IntegerOptionArgInfo WithValueName(string value) =>
            Update(value, Description, Visibility);

        public new IntegerOptionArgInfo WithDescription(string value) =>
            Update(ValueName, value, Visibility);

        public new IntegerOptionArgInfo WithVisibility(Visibility value) =>
            Update(ValueName, Description, value);

        protected override ArgInfo Update(string description, Visibility visibility) =>
            Update(ValueName, description, visibility);

        protected virtual IntegerOptionArgInfo Update(string valueName, string description, Visibility visibility) =>
            new IntegerOptionArgInfo(valueName, description, visibility);
    }
}
