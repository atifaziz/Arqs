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
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;

    public readonly struct ParseResult<T> : IEquatable<ParseResult<T>>
    {
        public ParseResult(T value) =>
            (Success, Value) = (true, value);

        public bool Success { get; }
        public T    Value   { get; }

        public override string ToString() =>
            Success ? $"{Value}" : string.Empty;

        public bool Equals(ParseResult<T> other) =>
            Success == other.Success && EqualityComparer<T>.Default.Equals(Value, other.Value);

        public override bool Equals(object obj) =>
            obj is ParseResult<T> other && Equals(other);

        public override int GetHashCode() =>
            Success ? EqualityComparer<T>.Default.GetHashCode(Value) : 0;

        public static bool operator ==(ParseResult<T> left, ParseResult<T> right) =>
            left.Equals(right);

        public static bool operator !=(ParseResult<T> left, ParseResult<T> right) =>
            !left.Equals(right);

        public static bool operator true(ParseResult<T> result) => result.Success;
        public static bool operator false(ParseResult<T> result) => !result.Success;

        public void Deconstruct(out bool success, out T value) =>
            (success, value) = (Success, Value);
    }

    public static class ParseResult
    {
        public static ParseResult<T> Success<T>(T value) => new ParseResult<T>(value);
    }

    public interface IParser
    {
        ParseResult<object> Parse(string text);
    }

    public interface IParser<T> : IParser
    {
        new ParseResult<T> Parse(string text);
    }

    public interface IParser<T, TOptions> : IParser<T>
    {
        TOptions Options { get; }
        IParser<T, TOptions> WithOptions(TOptions value);
    }

    public class GlobalParseOptions
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

    public class NumberParseOptions : GlobalParseOptions
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

        public DateTimeParseOptions Update(ImmutableArray<string> formats, DateTimeStyles styles, IFormatProvider formatProvider) =>
            UpdateCore(formats, styles, formatProvider);

        protected virtual DateTimeParseOptions UpdateCore(ImmutableArray<string> formats, DateTimeStyles styles, IFormatProvider formatProvider) =>
            new DateTimeParseOptions(formats, styles, formatProvider);

        protected override GlobalParseOptions UpdateCore(IFormatProvider formatProvider) =>
            UpdateCore(Formats, Styles, formatProvider);

        internal ParseResult<DateTime> ParseFormatted(string s) =>
            DateTime.TryParseExact(s, CachedFormatsArray, FormatProvider, Styles, out var v) ? ParseResult.Success(v) : default;
    }

    public static class Parser
    {
        static class Parsers
        {
            public static readonly IParser<string> Id = Create(ParseResult.Success);

            public static readonly IParser<int, NumberParseOptions> Int32 =
                Create(new NumberParseOptions(NumberStyles.Integer, CultureInfo.InvariantCulture),
                       (s, options) => int.TryParse(s, options.Styles, options.FormatProvider, out var v) ? ParseResult.Success(v) : default);

            public static readonly IParser<double, NumberParseOptions> Double =
                Create(new NumberParseOptions(NumberStyles.Float, CultureInfo.InvariantCulture),
                       (s, options) => double.TryParse(s, options.Styles, options.FormatProvider, out var v) ? ParseResult.Success(v) : default);

            public static readonly IParser<DateTime, DateTimeParseOptions> DateTime =
                Create(new DateTimeParseOptions(ImmutableArray<string>.Empty, DateTimeStyles.None, CultureInfo.InvariantCulture),
                       (s, options) => options.Formats.IsDefaultOrEmpty
                                     ? options.ParseFormatted(s)
                                     : System.DateTime.TryParse(s, options.FormatProvider, options.Styles, out var v) ? ParseResult.Success(v) : default);
        }

        public static IParser<int> Int32() => Parsers.Int32;
        public static IParser<int> Int32(NumberStyles styles) => Parsers.Int32.WithOptions(Parsers.Int32.Options.WithStyles(styles));
        public static IParser<double> Double() => Parsers.Double;
        public static IParser<double> Double(NumberStyles styles) => Parsers.Double.WithOptions(Parsers.Int32.Options.WithStyles(styles));
        public static IParser<DateTime> DateTime() => Parsers.DateTime;
        public static IParser<DateTime> DateTime(string format) => Parsers.DateTime.WithOptions(Parsers.DateTime.Options.WithFormats(ImmutableArray.Create(format)));
        public static IParser<string> String() => Parsers.Id;

        public static IParser<string> Literal(string value) =>
            Literal(value, StringComparison.Ordinal);

        public static IParser<string> Literal(string value, StringComparison comparison) =>
            Create(s => string.Equals(s, value, comparison) ? ParseResult.Success(s) : default);

        public static IParser<T> Range<T>(this IParser<T> parser, T min, T max) where T : IComparable<T> =>
            from v in parser
            select v.CompareTo(min) >= 0 && v.CompareTo(max) <= 0 ? v : throw new Exception();

        public static IParser<T> Cast<T>(this IParser parser) =>
            Create(s => parser.Parse(s) is (true, var v) ? ParseResult.Success((T)v) : default);

        public static IParser<T?> Nullable<T>(this IParser<T> parser) where T : struct =>
            parser.Cast<T?>();

        public static IParser<T> Create<T>(Func<string, ParseResult<T>> parser) =>
            new DelegatingParser<T>(parser);

        public static IParser<T, TOptions> Create<T, TOptions>(TOptions options, Func<string, TOptions, ParseResult<T>> parser) =>
            new DelegatingParser<T, TOptions>(options, parser);

        public static IParser<U> Select<T, U>(this IParser<T> parser, Func<T, U> f) =>
            Create(args => parser.Parse(args) is (true, var v) ? ParseResult.Success(f(v)) : default);

        public static IParser<U> SelectMany<T, U>(this IParser<T> parser, Func<T, IParser<U>> f) =>
            Create(args => parser.Parse(args) is (true, var v) ? f(v).Parse(args) : default);

        public static IParser<V> SelectMany<T, U, V>(this IParser<T> parser, Func<T, IParser<U>> f, Func<T, U, V> g) =>
            parser.Select(t => f(t).Select(u => g(t, u))).SelectMany(pv => pv);

        public static IParser<V>
            Join<T, U, _, V>(
                this IParser<T> first,
                IParser<U> second,
                Func<T, _> unused1, Func<U, _> unused2,
                Func<T, U, V> resultSelector) =>
            from e in first.Zip(second)
            select resultSelector(e.First, e.Second);

        public static IParser<(T First, U Second)>
            Zip<T, U>(this IParser<T> first, IParser<U> second) =>
            Create(s => first.Parse(s) is (true, var a) && second.Parse(s) is (true, var b) ? ParseResult.Success((a, b)) : default);

        sealed class DelegatingParser<T> : IParser<T>
        {
            readonly Func<string, ParseResult<T>> _parser;

            public DelegatingParser(Func<string, ParseResult<T>> parser) =>
                _parser = parser;

            public ParseResult<T> Parse(string text) =>
                _parser(text);

            ParseResult<object> IParser.Parse(string text) =>
                Parse(text) is (true, var v) ? ParseResult.Success((object)v) : default;
        }

        sealed class DelegatingParser<T, TOptions> : IParser<T, TOptions>
        {
            readonly Func<string, TOptions, ParseResult<T>> _parser;

            public DelegatingParser(TOptions options, Func<string, TOptions, ParseResult<T>> parser) =>
                (Options, _parser) = (options, parser);

            public TOptions Options { get; }

            public IParser<T, TOptions> WithOptions(TOptions value) =>
                new DelegatingParser<T, TOptions>(value, _parser);

            public ParseResult<T> Parse(string text) =>
                _parser(text, Options);

            ParseResult<object> IParser.Parse(string text) =>
                Parse(text) is (true, var v) ? ParseResult.Success((object)v) : default;
        }
    }
}
