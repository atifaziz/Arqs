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
    static class String
    {
        /// <summary>
        /// Returns a string that is a concatenation of two if none of them
        /// are <c>null</c>; otherwise returns <c>null</c>.
        /// </summary>

        public static string ConcatAll(string a, string b) =>
            a != null && b != null ? a + b : null;
    }
}
