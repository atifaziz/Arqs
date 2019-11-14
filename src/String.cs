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
    using System.Text;

    static class String
    {
        /// <summary>
        /// Returns a string that is a concatenation of two if none of them
        /// are <c>null</c>; otherwise returns <c>null</c>.
        /// </summary>

        public static string ConcatAll(string a, string b) =>
            a != null && b != null ? a + b : null;

        /// <summary>
        /// Returns a string that is a concatenation of three if none of them
        /// are <c>null</c>; otherwise returns <c>null</c>.
        /// </summary>

        public static string ConcatAll(string a, string b, string c) =>
            a != null && b != null && c != null ? a + b + c : null;

        /// <summary>
        /// Returns a string that is a concatenation of four if none of them
        /// are <c>null</c>; otherwise returns <c>null</c>.
        /// </summary>

        public static string ConcatAll(string a, string b, string c, string d) =>
            a != null && b != null && c != null && d != null ? a + b + c + d : null;

        /// <summary>
        /// Returns a string that is a concatenation of input strings if none
        /// of them  are <c>null</c>; otherwise returns <c>null</c>.
        /// </summary>

        public static string ConcatAll(params string[] strings)
        {
            foreach (var str in strings)
            {
                if (str == null)
                    return null;
            }

            return string.Concat(strings);
        }

        /// <summary>
        /// Returns a string that is a prefix prepended to a suffix if neither
        /// of them are <c>null</c>; otherwise returns <c>null</c>.
        /// </summary>

        public static string PrependAll(string suffix, string prefix) =>
            suffix != null && prefix != null ? suffix + prefix : null;

        /// <summary>
        /// Returns a string that is two prefixes prepended to a suffix if none
        /// of them are <c>null</c>; otherwise returns <c>null</c>.
        /// </summary>

        public static string PrependAll(string suffix, string prefix1, string prefix2) =>
            suffix != null && prefix1 != null && prefix2 != null ? prefix1 + prefix2 + suffix : null;

        /// <summary>
        /// Returns a string that is three prefixes prepended to a suffix if
        /// none of them are <c>null</c>; otherwise returns <c>null</c>.
        /// </summary>

        public static string PrependAll(string suffix, string prefix1, string prefix2, string c) =>
            suffix != null && prefix1 != null && prefix2 != null && c != null ? prefix1 + prefix2 + c + suffix : null;

        /// <summary>
        /// Returns a string that is prefixes prepended to a suffix if none of
        /// them are <c>null</c>; otherwise returns <c>null</c>.
        /// </summary>

        public static string PrependAll(string suffix, params string[] prefixes)
        {
            if (suffix == null)
                return null;

            foreach (var prefix in prefixes)
            {
                if (prefix == null)
                    return null;
            }

            var sb = new StringBuilder(suffix);
            foreach (var prefix in prefixes)
                sb.Append(prefix);

            return sb.Append(suffix).ToString();
        }
    }
}
