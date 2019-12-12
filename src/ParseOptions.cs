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
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;

    public class GlobalParseOptions
    {
        public GlobalParseOptions(IFormatProvider formatProvider) =>
            FormatProvider = formatProvider;

        public IFormatProvider FormatProvider { get; }

        public GlobalParseOptions WithFormatProvider(IFormatProvider value) =>
            Update(value);

        protected virtual GlobalParseOptions Update(IFormatProvider formatProvider) =>
            formatProvider == FormatProvider ? this : new GlobalParseOptions(formatProvider);
    }

    public class NumberParseOptions : GlobalParseOptions
    {
        public NumberParseOptions(NumberStyles styles, IFormatProvider formatProvider) :
            base(formatProvider) => Styles = styles;

        public NumberStyles Styles { get; }

        public NumberParseOptions WithStyles(NumberStyles value) =>
            Update(value, FormatProvider);

        public new NumberParseOptions WithFormatProvider(IFormatProvider value) =>
            Update(Styles, value);

        protected override GlobalParseOptions Update(IFormatProvider formatProvider) =>
            Update(Styles, formatProvider);

        protected virtual NumberParseOptions Update(NumberStyles styles, IFormatProvider formatProvider) =>
            new NumberParseOptions(styles, formatProvider);
    }

    public class DateTimeParseOptions : GlobalParseOptions
    {
        string[] _cachedFormatsArray;

        public DateTimeParseOptions(ImmutableArray<string> formats, DateTimeStyles styles,
            IFormatProvider formatProvider) :
            base(formatProvider)
        {
            Styles = styles;
            Formats = formats;
        }

        public ImmutableArray<string> Formats { get; }

        string[] CachedFormatsArray => _cachedFormatsArray ??= Formats.ToArray();

        public DateTimeStyles Styles { get; }

        public DateTimeParseOptions WithFormats(ImmutableArray<string> value) =>
            Update(value, Styles, FormatProvider);

        public DateTimeParseOptions WithStyles(DateTimeStyles value) =>
            Update(Formats, value, FormatProvider);

        public new DateTimeParseOptions WithFormatProvider(IFormatProvider value) =>
            Update(Formats, Styles, value);

        protected override GlobalParseOptions Update(IFormatProvider formatProvider) =>
            Update(Formats, Styles, formatProvider);

        protected virtual DateTimeParseOptions Update(ImmutableArray<string> formats, DateTimeStyles styles, IFormatProvider formatProvider) =>
            new DateTimeParseOptions(formats, styles, formatProvider);

        internal ParseResult<DateTime> ParseFormatted(string s) =>
            DateTime.TryParseExact(s, CachedFormatsArray, FormatProvider, Styles, out var v) ? ParseResult.Success(v) : default;
    }
}
