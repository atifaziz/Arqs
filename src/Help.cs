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
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public static class Help
    {
        public static readonly ICli<string> BlankLine = CreateText(string.Empty);

        public static ICli<string> Text(string line) =>
            string.IsNullOrEmpty(line) ? BlankLine : CreateText(line);

        static ICli<string> CreateText(string line)
        {
            IEnumerable<ICliRecord> singleton = ImmutableArray.Create(CliRecord.Text(line));
            return Cli.Create(_ => line, () => singleton);
        }

        public static ICli<ImmutableArray<string>> Text(params string[] lines)
        {
            IEnumerable<ICliRecord> rs = null;
            return Cli.Create(_ => ImmutableArray.Create(lines),
                () => rs ??= from line in lines
                    select CliRecord.Text(line));
        }
    }
}
