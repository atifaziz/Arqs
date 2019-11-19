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
    using System.Linq;

    partial interface IParser
    {
        object Parse(string text);
    }

    partial interface IParser<out T> : IParser
    {
        new T Parse(string text);
    }

    partial interface IParser<out T, TOptions> : IParser<T>
    {
        TOptions Options { get; }
        IParser<T, TOptions> WithOptions(TOptions value);
    }

    partial class GlobalParseOptions
    {
        public GlobalParseOptions(IFormatProvider formatProvider) =>
            FormatProvider = formatProvider;

        public IFormatProvider FormatProvider { get; }

        public GlobalParseOptions WithFormatProvider(IFormatProvider value) =>
            Update(value);

        public GlobalParseOptions Update(IFormatProvider formatProvider) =>
            UpdateCore(formatProvider);

        protected virtual GlobalParseOptions UpdateCore(IFormatProvider formatProvider) =>
            formatProvider == FormatProvider ? this : new GlobalParseOptions(formatProvider);
    }

    partial class NumberParseOptions : GlobalParseOptions
    {
        public NumberParseOptions(NumberStyles styles, IFormatProvider formatProvider) :
            base(formatProvider) => Styles = styles;

        public NumberStyles Styles { get; }

        public NumberParseOptions WithStyles(NumberStyles value) =>
            Update(value, FormatProvider);

        public new NumberParseOptions WithFormatProvider(IFormatProvider value) =>
            Update(Styles, value);

        public NumberParseOptions Update(NumberStyles styles, IFormatProvider formatProvider) =>
            UpdateCore(styles, formatProvider);

        protected virtual NumberParseOptions UpdateCore(NumberStyles styles, IFormatProvider formatProvider) =>
            new NumberParseOptions(styles, formatProvider);

        protected override GlobalParseOptions UpdateCore(IFormatProvider formatProvider) =>
            UpdateCore(Styles, formatProvider);
    }

    partial class DateTimeParseOptions : GlobalParseOptions
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

        public DateTimeParseOptions Update(ImmutableArray<string> formats, DateTimeStyles styles, IFormatProvider formatProvider) =>
            UpdateCore(formats, styles, formatProvider);

        protected virtual DateTimeParseOptions UpdateCore(ImmutableArray<string> formats, DateTimeStyles styles, IFormatProvider formatProvider) =>
            new DateTimeParseOptions(formats, styles, formatProvider);

        protected override GlobalParseOptions UpdateCore(IFormatProvider formatProvider) =>
            UpdateCore(Formats, Styles, formatProvider);

        internal DateTime ParseFormatted(string s) =>
            DateTime.ParseExact(s, CachedFormatsArray, FormatProvider, Styles);
    }

    static partial class Parser
    {
        static class Parsers
        {
            public static readonly IParser<string> Id = Create(s => s);

            public static readonly IParser<int, NumberParseOptions> Int32 =
                Create(new NumberParseOptions(NumberStyles.Integer, CultureInfo.InvariantCulture),
                       (s, options) => int.Parse(s, options.Styles, options.FormatProvider));

            public static readonly IParser<double, NumberParseOptions> Double =
                Create(new NumberParseOptions(NumberStyles.Float, CultureInfo.InvariantCulture),
                       (s, options) => double.Parse(s, options.Styles, options.FormatProvider));

            public static readonly IParser<DateTime, DateTimeParseOptions> DateTime =
                Create(new DateTimeParseOptions(ImmutableArray<string>.Empty, DateTimeStyles.None, CultureInfo.InvariantCulture),
                       (s, options) => options.Formats.IsDefaultOrEmpty
                                     ? options.ParseFormatted(s)
                                     : System.DateTime.Parse(s, options.FormatProvider, options.Styles));
        }

        public static IParser<int> Int32() => Parsers.Int32;
        public static IParser<int> Int32(NumberStyles styles) => Create(s => int.Parse(s, styles, CultureInfo.InvariantCulture));
        public static IParser<double> Double() => Parsers.Double;
        public static IParser<double> Double(NumberStyles styles) => Create(s => double.Parse(s, styles, CultureInfo.InvariantCulture));
        public static IParser<DateTime> DateTime() => Parsers.DateTime;
        public static IParser<DateTime> DateTime(string format) => Parsers.DateTime.WithOptions(Parsers.DateTime.Options.WithFormats(ImmutableArray.Create(format)));
        public static IParser<string> String() => Parsers.Id;

        public static IParser<T> Range<T>(this IParser<T> parser, T min, T max) where T : IComparable<T> =>
            from v in parser
            select v.CompareTo(min) >= 0 && v.CompareTo(max) <= 0 ? v : throw new Exception();

        public static IParser<T> Cast<T>(this IParser parser) =>
            Create(s => (T)parser.Parse(s));

        public static IParser<T?> Nullable<T>(this IParser<T> parser) where T : struct =>
            parser.Cast<T?>();

        public static IParser<T> Create<T>(Func<string, T> parser) =>
            new DelegatingParser<T>(parser);

        public static IParser<T, TOptions> Create<T, TOptions>(TOptions options, Func<string, TOptions, T> parser) =>
            new DelegatingParser<T, TOptions>(options, parser);

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

            object IParser.Parse(string text) => Parse(text);
        }

        sealed class DelegatingParser<T, TOptions> : IParser<T, TOptions>
        {
            readonly Func<string, TOptions, T> _parser;

            public DelegatingParser(TOptions options, Func<string, TOptions, T> parser) =>
                (Options, _parser) = (options, parser);

            public TOptions Options { get; }

            public IParser<T, TOptions> WithOptions(TOptions value) =>
                new DelegatingParser<T, TOptions>(value, _parser);

            public T Parse(string text) =>
                _parser(text, Options);

            object IParser.Parse(string text) => Parse(text);
        }
    }
}
