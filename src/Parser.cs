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
    using System;
    using System.Collections.Immutable;
    using System.Globalization;

    partial interface IParser<out T>
    {
        T Parse(string text);
    }

    partial interface IParser<in TOptions, out T>
    {
        T Parse(string text, TOptions options);
    }

    partial class CulturalParserOptions
    {
        public CulturalParserOptions(IFormatProvider formatProvider) =>
            FormatProvider = formatProvider;

        public IFormatProvider FormatProvider { get; }

        public CulturalParserOptions Update(IFormatProvider formatProvider) =>
            UpdateCulturalParserOptions(formatProvider);

        protected virtual CulturalParserOptions UpdateCulturalParserOptions(IFormatProvider formatProvider) =>
            formatProvider == FormatProvider ? this : new CulturalParserOptions(formatProvider);
    }

    partial class NumberParserOptions : CulturalParserOptions
    {
        public NumberParserOptions(NumberStyles styles, IFormatProvider formatProvider) :
            base(formatProvider) => Styles = styles;

        NumberStyles Styles { get; }

        public NumberParserOptions Update(NumberStyles styles, IFormatProvider formatProvider) =>
            new NumberParserOptions(styles, formatProvider);

        protected override CulturalParserOptions UpdateCulturalParserOptions(IFormatProvider formatProvider) =>
            new NumberParserOptions(Styles, formatProvider);
    }

    partial class DateTimeParserOptions : CulturalParserOptions
    {
        public DateTimeParserOptions(ImmutableArray<string> formats, DateTimeStyles styles,
            IFormatProvider formatProvider) :
            base(formatProvider)
        {
            Styles = styles;
            Formats = formats;
        }

        ImmutableArray<string> Formats { get; }
        DateTimeStyles Styles { get; }

        public DateTimeParserOptions Update(ImmutableArray<string> formats, DateTimeStyles styles, IFormatProvider formatProvider) =>
            new DateTimeParserOptions(formats, styles, formatProvider);

        protected override CulturalParserOptions UpdateCulturalParserOptions(IFormatProvider formatProvider) =>
            new DateTimeParserOptions(Formats, Styles, formatProvider);
    }

    partial interface ICulturalParser<out T> : IParser<T>
    {
        IFormatProvider FormatProvider { get; }
        ICulturalParser<T> WithFormatProvider(IFormatProvider value);
    }

    partial interface INumberParser<out T> : ICulturalParser<T>
    {
        NumberStyles Styles { get; }
        INumberParser<T> WithNumberStyles(NumberStyles value);
        new INumberParser<T> WithFormatProvider(IFormatProvider value);
    }

    partial interface IDateTimeParser<out T> : ICulturalParser<T>
    {
        ImmutableArray<string> Formats { get; }
        DateTimeStyles Styles { get; }
        IDateTimeParser<T> WithFormats(ImmutableArray<string> formats);
        new IDateTimeParser<T> WithFormatProvider(IFormatProvider value);
    }

    static partial class Parser
    {
        static class Stock
        {
            public static readonly IParser<string> Id = Create(s => s);
            public static readonly IParser<int> Int = Create(s => int.Parse(s, CultureInfo.InvariantCulture));
            public static readonly IParser<int> PositiveInt = Create(s => int.Parse(s, NumberStyles.None, CultureInfo.InvariantCulture));
        }

        public static IParser<int> Int() => Stock.Int;
        public static IParser<int> Int(NumberStyles styles) => Create(s => int.Parse(s, styles, CultureInfo.InvariantCulture));
        public static IParser<int> PositiveInt(NumberStyles styles) => Create(s => int.Parse(s, styles, CultureInfo.InvariantCulture));
        public static IParser<string> String() => Stock.Id;

        public static IParser<T> Create<T>(Func<string, T> parser) =>
            new DelegatingParser<T>(parser);

        public static IParser<U> Select<T, U>(this IParser<T> parser, Func<T, U> f) =>
            Create(args => f(parser.Parse(args)));

        public static IParser<U> SelectMany<T, U>(this IParser<T> parser, Func<T, IParser<U>> f) =>
            Create(args => f(parser.Parse(args)).Parse(args));

        public static IParser<V> SelectMany<T, U, V>(this IParser<T> parser, Func<T, IParser<U>> f, Func<T, U, V> g) =>
            parser.Select(t => f(t).Select(u => g(t, u))).SelectMany(pv => pv);

        sealed class DelegatingParser<T> : IParser<T>
        {
            readonly Func<string, T> _parser;

            public DelegatingParser(Func<string, T> parser) =>
                _parser = parser;

            public T Parse(string text) =>
                _parser(text);
        }

        partial class NumberParser<T> : IParser<T>
        {
            readonly NumberStyles _styles;
            readonly Func<string, NumberStyles, T> _parser;

            public NumberParser(NumberStyles styles, Func<string, NumberStyles, T> parser) =>
                _parser = parser;

            public T Parse(string text) =>
                _parser(text, _styles);
        }
    }
}
