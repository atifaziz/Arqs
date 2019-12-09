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

    public interface IInspectionRecord
    {
        T Match<T>(Func<IArg, T> argSelector,
                   Func<string, T> textSelector);
    }

    public static class InspectionRecord
    {
        static readonly IInspectionRecord BlankText = Text(string.Empty);

        public static IInspectionRecord Text(string text) =>
            string.IsNullOrEmpty(text) ? BlankText : new TextInspectionRecord(text);

        sealed class TextInspectionRecord : IInspectionRecord
        {
            readonly string _text;

            public TextInspectionRecord(string text) => _text = text;

            public T Match<T>(Func<IArg, T> argSelector, Func<string, T> textSelector) =>
                textSelector(_text);
        }

        public static IEnumerable<IArg> InspectArgs(this IArgBinder binder) =>
            from ir in binder.Inspect()
            select ir.Match(arg => arg, _ => null) into arg
            where arg != null
            select arg;
    }
}
