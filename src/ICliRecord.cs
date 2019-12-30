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
    using System.Linq;

    public interface ICliRecord
    {
        T Match<T>(Func<IArg, T> argSelector,
                   Func<string, T> textSelector);
    }

    public static class CliRecord
    {
        static readonly ICliRecord BlankText = new TextCliRecord(string.Empty);

        public static ICliRecord Text(string text) =>
            string.IsNullOrEmpty(text) ? BlankText : new TextCliRecord(text);

        sealed class TextCliRecord : ICliRecord
        {
            readonly string _text;

            public TextCliRecord(string text) => _text = text;

            public T Match<T>(Func<IArg, T> argSelector, Func<string, T> textSelector) =>
                textSelector(_text);
        }

        public static IEnumerable<IArg> GetArgs(this ICli cli) =>
            from ir in cli.Inspect()
            select ir.Match(arg => arg, _ => null) into arg
            where arg != null
            select arg;
    }
}
