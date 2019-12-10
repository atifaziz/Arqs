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
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public static class CommandLine
    {
        static readonly string UnbundledValueReference = new string('*', 1);

        public static (T Result, ImmutableArray<string> Tail)
            Bind<T>(this IArgBinder<T> binder, params string[] args)
        {
            var specs = binder.GetArgs().ToList();

            var accumulators = new IAccumulator[specs.Count];
            for (var i = 0; i < specs.Count; i++)
                accumulators[i] = specs[i].CreateAccumulator();

            var asi = 0;
            var nsi = 0;
            var tail = new List<string>();

            using var reader = args.Read();
            while (reader.TryPeek(out var arg))
            {
                (string, ShortOptionName) name = default;

                if (arg.Length > 1 && arg[0] == '@')
                {
                    var i = specs.FindIndex(e => e.IsMacro());
                    if (i >= 0)
                    {
                        reader.Unread(reader.Read().Substring(1));
                        if (!accumulators[i].Accumulate(reader))
                            throw new Exception("Invalid macro: " + arg);
                        continue;
                    }
                }

                if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    var longName = arg.Substring(2);
                    if (longName.Length > 2)
                    {
                        char lch;
                        var equalIndex = longName.IndexOf('=');
                        if (equalIndex > 0)
                        {
                            reader.Read();
                            reader.Unread(longName.Substring(equalIndex + 1));
                            reader.Unread(UnbundledValueReference);
                            reader.Unread(arg);
                            longName = longName.Substring(0, equalIndex);
                        }
                        else if ((lch = longName[longName.Length - 1]) == '+' || lch == '-')
                        {
                            reader.Read();
                            reader.Unread(lch.ToString());
                            reader.Unread(UnbundledValueReference);
                            reader.Unread(longName = longName.Substring(0, longName.Length - 1));
                        }
                    }

                    name = (longName, null);
                }
                else if (arg.Length > 1 && arg[0] == '-')
                {
                    if (IsDigital(arg, 1, arg.Length))
                    {
                        var i = specs.FindIndex(nsi, e => e.IsIntegerOption());
                        if (i >= 0)
                        {
                            reader.Unread(reader.Read().Substring(1));
                            if (!accumulators[i].Accumulate(reader))
                                throw new Exception("Invalid option: " + arg);
                            nsi = i + 1;
                        }
                        else
                        {
                            tail.Add(reader.Read());
                        }

                        continue;
                    }

                    if (arg.Length > 2)
                    {
                        reader.Read();
                        var unreads = new Stack<string>();
                        int j;
                        for (j = 1; j < arg.Length; j++)
                        {
                            var ch = arg[j];
                            var i = specs.FindIndex(e => e.ShortName() is ShortOptionName sn && (char)sn == ch);
                            if (i >= 0)
                            {
                                var spec = specs[i];
                                if (spec.Info is OptionArgInfo)
                                {
                                    unreads.Push("-" + ch);
                                    if (j + 1 < arg.Length)
                                    {
                                        unreads.Push(UnbundledValueReference);
                                        unreads.Push(arg.Substring(j + 1));
                                    }
                                    break;
                                }

                                unreads.Push("-" + ch);

                                char nch;
                                if (spec.IsFlag() && j + 1 < arg.Length && ((nch = arg[j + 1]) == '+' || nch == '-'))
                                {
                                    unreads.Push(UnbundledValueReference);
                                    unreads.Push(nch.ToString());
                                    j++;
                                }
                            }
                            else
                            {
                                throw new Exception("Invalid option: " + ch);
                            }
                        }

                        while (unreads.Count > 0)
                            reader.Unread(unreads.Pop());

                        continue;
                    }

                    var snch = arg[1];
                    if (!ShortOptionName.TryParse(snch, out var sn))
                        throw new Exception("Invalid option: " + snch);

                    name = (null, sn);
                }

                if (name == default)
                {
                    var i = specs.FindIndex(asi, e => e.IsOperand());
                    if (i >= 0)
                    {
                        asi = i + 1;
                        if (!accumulators[i].Accumulate(reader))
                            throw new Exception("Invalid argument: " + arg);
                    }
                    else
                    {
                        reader.Read();
                        tail.Add(arg);
                    }
                }
                else
                {
                    var i = specs.FindIndex(e => name switch
                    {
                        (string ln, null) => e.LongName() == ln,
                        (null, ShortOptionName sn) => e.ShortName() == sn,
                        _ => false,
                    });

                    if (i < 0 && name is (string tln, null)
                              && tln.Length > 3
                              && tln.StartsWith("no-", StringComparison.Ordinal))
                    {
                        var rln = tln.Substring(3);
                        i = specs.FindIndex(e => e.IsNegtableFlag() && e.LongName() == rln);
                        if (i >= 0)
                        {
                            var lch = arg[arg.Length - 1];

                            if (lch == '+' || lch == '-')
                                throw new Exception("Invalid flag: " + arg);

                            reader.Read();
                            reader.Unread("-");
                            reader.Unread(UnbundledValueReference);
                            reader.Unread(arg);
                        }
                    }

                    if (i >= 0)
                    {
                        reader.Read();

                        var isValueUnbundled = false;
                        if (reader.TryPeek(out var uvr) && ReferenceEquals(uvr, UnbundledValueReference))
                        {
                            isValueUnbundled = true;
                            reader.Read();
                        }

                        if ((specs[i].IsFlag() || specs[i].Info is OptionArgInfo info && info.IsValueOptional)
                            && !isValueUnbundled)
                        {
                            accumulators[i].AccumulateDefault();
                        }
                        else if (!accumulators[i].Accumulate(reader))
                        {
                            var (ln, sn) = name;
                            throw new Exception("Invalid value for option: " + (ln ?? sn.ToString()));
                        }
                    }
                    else
                    {
                        var (ln, sn) = name;
                        throw new Exception("Invalid option: " + (ln ?? sn.ToString()));
                    }
                }
            }

            var ar = accumulators.Read();
            return (binder.Bind(() => ar.Read()), tail.ToImmutableArray());

            static bool IsDigital(string s, int start, int end)
            {
                for (var i = start; i < end; i++)
                {
                    if (s[i] < '0' || s[i] > '9')
                        return false;
                }
                return true;
            }
        }
    }
}
