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
    using System.Linq;

    public sealed class OptionNames
    {
        static readonly OptionNames[] ShortNamesCache;

        static OptionNames()
        {
            ShortNamesCache = new OptionNames['z' + 1];
            InitShortNamesCacheRange('0', '9');
            InitShortNamesCacheRange('A', 'Z');
            InitShortNamesCacheRange('a', 'z');

            static void InitShortNamesCacheRange(char first, char last)
            {
                for (var ch = first; ch <= last; ch++)
                    ShortNamesCache[ch] = new OptionNames(ShortOptionName.Parse(ch), null, null);
            }
        }
        public static OptionNames Short(char shortName) =>
            All(shortName, null, null);

        public static OptionNames Short(ShortOptionName shortName) =>
            All(shortName, null, null);

        public static OptionNames ShortLong(char shortName, string longName) =>
            All(shortName, longName, null);

        public static OptionNames ShortLong(ShortOptionName shortName, string longName) =>
            All(shortName, longName, null);

        public static OptionNames LongAbbreviated(string longName, string abbreviatedName) =>
            All(null, longName, abbreviatedName);

        public static OptionNames All(char shortName, string longName, string abbreviatedName) =>
            All(ShortOptionName.Parse(shortName), longName, abbreviatedName);

        public static OptionNames All(ShortOptionName shortName, string longName, string abbreviatedName) =>
            longName == null && abbreviatedName == null && shortName != null
            ? ShortNamesCache[(char)shortName]
            : new OptionNames(shortName, longName, abbreviatedName);

        OptionNames(ShortOptionName shortName, string longName, string abbreviatedName)
        {
            if (longName != null && longName.Length < 2)
                throw new ArgumentException("The long name of an option must be greater than a character in length when supplied.", nameof(longName));

            if (abbreviatedName != null)
            {
                if (longName == null)
                    throw new ArgumentNullException(nameof(longName));
                if (abbreviatedName.Length >= longName.Length)
                    throw new ArgumentException("The abbreviated name of an option cannot be longer than its long name.", nameof(abbreviatedName));
            }

            ShortName = shortName;
            LongName = longName;
            AbbreviatedName = abbreviatedName;
        }

        public ShortOptionName ShortName { get; }
        public string LongName { get; }
        public string AbbreviatedName { get; }

        public OptionNames WithShortName(char value) =>
            WithShortName(ShortOptionName.Parse(value));

        public OptionNames WithShortName(ShortOptionName value) =>
            All(value, LongName, AbbreviatedName);

        public OptionNames WithLongName(string value) =>
            All(ShortName, value, AbbreviatedName);

        public OptionNames WithAbbreviatedName(string value) =>
            All(ShortName, LongName, value);

        public override string ToString() =>
            string.Join(", ",
                from n in new[]
                {
                    String.ConcatAll("-", ShortName?.ToString()),
                    String.ConcatAll("--", AbbreviatedName),
                    String.ConcatAll("--", LongName),
                }
                where n != null
                select n);

        public int Count
            => (ShortName       != null ? 1 : 0)
             + (LongName        != null ? 1 : 0)
             + (AbbreviatedName != null ? 1 : 0);

        public static OptionNames Guess(string name1, string name2, string name3)
        {
            ShortOptionName s = null;
            string m = null, l = null;

            for (var i = 0; i < 3; i++)
            {
                var name = i switch { 0 => name1, 1 => name2, _ => name3 };
                if (string.IsNullOrEmpty(name))
                    continue;
                if (name.Length == 1)
                {
                    if (s != null)
                        throw DuplicateError(i, name);
                    s = ShortOptionName.Parse(name[0]);
                }
                else
                {
                    if (l == null)
                    {
                        l = name;
                    }
                    else
                    {
                        if (l == name)
                            throw DuplicateError(i, name);

                        if (name.Length > l.Length)
                        {
                            m = l;
                            l = name;
                        }
                        else
                        {
                            if (m != null)
                                throw DuplicateError(i, name);
                            m = name;
                        }
                    }
                }
            }

            return All(s, l, m);

            static ArgumentException DuplicateError(int i, string name) =>
                throw new ArgumentException("Duplicate argument name: " + name,
                                            i switch { 0 => nameof(name1),
                                                       1 => nameof(name2),
                                                       _ => nameof(name3) });
        }
    }
}
