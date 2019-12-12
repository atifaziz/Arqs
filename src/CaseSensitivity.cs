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
    using static CaseSensitivity;

    public enum CaseSensitivity
    {
        CaseSensitive,
        CaseInsensitive,
    }

    public static class CaseSensitivityExtensions
    {
        public static StringComparison ToStringComparison(this CaseSensitivity caseSensitivity) =>
            caseSensitivity switch
            {
                CaseSensitive   => StringComparison.Ordinal,
                CaseInsensitive => StringComparison.OrdinalIgnoreCase,
                _ => throw new ArgumentOutOfRangeException(nameof(caseSensitivity), caseSensitivity, null)
            };

        public static StringComparer ToStringComparer(this CaseSensitivity caseSensitivity) =>
            caseSensitivity switch
            {
                CaseSensitive   => StringComparer.Ordinal,
                CaseInsensitive => StringComparer.OrdinalIgnoreCase,
                _ => throw new ArgumentOutOfRangeException(nameof(caseSensitivity), caseSensitivity, null)
            };
    }
}
