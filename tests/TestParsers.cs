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

namespace Arqs.Tests
{
    using NUnit.Framework;
    using System;
    using System.Globalization;

    public class TestParsers
    {
        public class DateTimeParser
        {
            [TestCase(";", "yyyy-MM-dd HH:mm", "2019-12-13", false, "0001-01-01T00:00:00")]
            public void DateTimeWithFormats(string formatsDelimiter,
                                            string formats,
                                            string input,
                                            bool expectedSuccess,
                                            string expectedValue)
            {
                var (success, value) =
                    Parser.DateTime()
                          .Format(formats.Split(formatsDelimiter))
                          .Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value.ToString("s"), Is.EqualTo(expectedValue));
            }

            [TestCase("yyyy-MM-dd",       "2020-02-10",       true,  "2020-02-10T00:00:00", Description = "Simple date format")]
            [TestCase("yyyy-MM-dd HH:mm", "2020-02-10 23:10", true,  "2020-02-10T23:10:00", Description = "Date with time format")]
            [TestCase("yyyy-MM-dd",       "2020-13-10",       false, "0001-01-01T00:00:00", Description = "Invalid input - month")]
            public void DateTimeFormat(string format,
                                       string input,
                                       bool expectedSuccess,
                                       string expectedValue)
            {
                var (success, value) =
                    Parser.DateTime(format)
                          .Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value.ToString("s"), Is.EqualTo(expectedValue));
            }
        }

        public class Int32
        {
            [TestCase("2",          true,  2,    Description = "Positive number")]
            [TestCase("-232",       true,  -232, Description = "Negative number")]
            [TestCase("2147483648", false, 0,    Description = "Out of range")]
            [TestCase("3x",         false, 0,    Description = "Invalid number")]
            [TestCase("x3",         false, 0,    Description = "Invalid number")]
            [TestCase("+6",         true,  6,    Description = "Leading sign")]
            public void Input(string input, bool expectedSuccess, int expectedValue)
            {
                var (success, value) = Parser.Int32().Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value, Is.EqualTo(expectedValue));
            }

            [TestCase("10", true, 16, NumberStyles.AllowHexSpecifier, Description = "Hex number")]
            public void Styles(string input, bool expectedSuccess, int expectedValue, NumberStyles styles)
            {
                var baseParser = Parser.Int32();
                var parser = baseParser.Styles(styles);
                var (success, value) = parser.Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value, Is.EqualTo(expectedValue));
                Assert.That(parser, Is.Not.SameAs(baseParser));
            }

            [Test]
            public void Options()
            {
                var options = new NumberParseOptions(NumberStyles.Number, CultureInfo.GetCultureInfo("de-AT"));
                var baseParser = Parser.Int32();
                var parser = baseParser.WithOptions(options);
                var (success, value) = parser.Parse("234");
                Assert.That(success, Is.EqualTo(true));
                Assert.That(value, Is.EqualTo(234));
                Assert.That(parser, Is.Not.SameAs(baseParser));
                Assert.That(parser.Options, Is.SameAs(options));
            }
        }

        public class Double
        {
            [TestCase("2.2",    true,  2.2,    Description = "Positive number")]
            [TestCase("-232.1", true,  -232.1, Description = "Negative number")]
            [TestCase("3.1x",   false, 0,      Description = "Invalid number")]
            [TestCase("x3",     false, 0,      Description = "Invalid number")]
            [TestCase("+6.2",   true,  6.2,    Description = "Leading sign")]
            [TestCase("5.",     true,  5.0,    Description = "Incomplete double")]
            public void Input(string input, bool expectedSuccess, double expectedValue)
            {
                var (success, value) =
                    Parser.Double()
                          .Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value, Is.EqualTo(expectedValue));
            }
        }

        public class DelimiterTests
        {
            [TestCase(',', "10,15,12",    true,  new[] { 10, 15, 12 },     Description = "Comma separated")]
            [TestCase(';', "11;22;33",    true,  new[] { 11, 22, 33 },     Description = "Semicolon separated")]
            [TestCase(',', "-1,-2,-3,-6", true,  new[] { -1, -2, -3, -6 }, Description = "Negative numbers")]
            [TestCase(',', "20",          true,  new[] { 20 },             Description = "One element")]
            [TestCase(',', "220,",        true,  new[] { 220 },            Description = "One element ending with separator")]
            [TestCase(',', "10,15x,12",   false, null,                     Description = "Invalid element in array")]
            public void Int32(char delimiter, string input, bool expectedSuccess, int[] expectedValue)
            {
                var (success, value) = Parser.Int32().Delimited(delimiter).Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value, Is.EqualTo(expectedValue));
            }
        }


        public class RangeTests
        {
            [TestCase(0, 10,     "8",    true,  8,   Description = "In range")]
            [TestCase(-100, -22, "-22",  true,  -22, Description = "Upper range limit")]
            [TestCase(203, 204,  "203",  true,  203, Description = "Lower range limit")]
            [TestCase(13, 14,    "15",   false, 0,   Description = "Out of range")]
            [TestCase(5, 0,      "3",    false, 0,   Description = "Invalid range")]
            public void Int32(int lowerLimit,
                              int upperLimit,
                              string input,
                              bool expectedSuccess,
                              int expectedValue)
            {
                var (success, value) =
                    Parser.Int32()
                          .Range(lowerLimit, upperLimit)
                          .Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value, Is.EqualTo(expectedValue));
            }

            [TestCase(8.1, 8.3,     "8.234",    true,  8.234,    Description = "In range")]
            [TestCase(-5, -2.2,     "-2.32",    true,  -2.32,    Description = "Upper range limit")]
            [TestCase(203.310, 204, "203.310",  true,  203.310,  Description = "Lower range limit")]
            [TestCase(13.1, 14.1,   "15.1",     false, 0,        Description = "Out of range")]
            [TestCase(4.5, -0.1,    "3.1",      false, 0,        Description = "Invalid range")]
            public void Double(double lowerLimit,
                               double upperLimit,
                               string input,
                               bool expectedSuccess,
                               double expectedValue)
            {
                var (success, value) =
                    Parser.Double()
                          .Range(lowerLimit, upperLimit)
                          .Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value, Is.EqualTo(expectedValue));
            }

            [TestCase("2019-12-10", "2019-12-31", "2019-12-15", true,  "2019-12-15T00:00:00", Description = "In range")]
            [TestCase("2019-12-10", "2019-12-31", "2019-12-01", false, "0001-01-01T00:00:00", Description = "Out of range")]
            [TestCase("2019-12-10", "2019-12-01", "2019-12-11", false, "0001-01-01T00:00:00", Description = "Invalid range")]
            [TestCase("2019-12-10", "2019-12-31", "2019-12-10", true,  "2019-12-10T00:00:00", Description = "On lower edge")]
            [TestCase("2019-12-10", "2019-12-31", "2019-12-31", true,  "2019-12-31T00:00:00", Description = "On upper edge")]
            public void DateTimeRange(string lowerLimit,
                                      string upperLimit,
                                      string input,
                                      bool expectedSuccess,
                                      string expectedValue)
            {
                var (success, value) =
                    Parser.DateTime()
                          .Range(DateTime.ParseExact(lowerLimit, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                                 DateTime.ParseExact(upperLimit, "yyyy-MM-dd", CultureInfo.InvariantCulture))
                          .Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value.ToString("s"), Is.EqualTo(expectedValue));
            }
        }

        public class ChoiceTests
        {
            [TestCase(new string[] { "foo", "bar" },          "foo",  true, "foo", Description = "String choice valid selection")]
            [TestCase(new string[] { "one", "two", "three" }, "four", false, null, Description = "Invalid string choice selection")]
            public void StringChoices(string[] choices,
                                      string input,
                                      bool expectedSuccess,
                                      string expectedValue)
            {
                var (success, value) =
                    Parser.String()
                          .Choose(choices)
                          .Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value, Is.EqualTo(expectedValue));
            }

            [TestCase(new string[] { "foo", "bar" }, CaseSensitivity.CaseInsensitive, "foo", true, "foo", Description = "String choice valid selection")]
            [TestCase(new string[] { "foo", "bar" }, CaseSensitivity.CaseSensitive,   "Bar", false, null, Description = "Invalid string choice selection")]
            public void StringChoiceCaseSensitivity(string[] choices,
                                                    CaseSensitivity comparer,
                                                    string input,
                                                    bool expectedSuccess,
                                                    string expectedValue)
            {
                var (success, value) =
                    Parser.String()
                          .Choose(comparer.ToStringComparer(), choices)
                          .Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value, Is.EqualTo(expectedValue));
            }

            [TestCase(new int[] { 10, 20 },  "10", true,  10, Description = "Int choice valid selection")]
            [TestCase(new int[] { 1, 2, 3 }, "4",  false, 0,  Description = "Invalid Int choice selection")]
            [TestCase(new int[] { 1, 2, 3 }, "1",  true,  1,  Description = "First element Int choice selection")]
            [TestCase(new int[] { 1, 2, 3 }, "3",  true,  3,  Description = "Last element Int choice selection")]
            [TestCase(new int[] { 5 },       "5",  true,  5,  Description = "Single Int choice")]
            [TestCase(new int[] { 5 },       "7",  false, 0,  Description = "Single Int choice with bad input")]
            public void IntChoices(int[] choices,
                                   string input,
                                   bool expectedSuccess,
                                   int expectedValue)
            {
                var (success, value) =
                    Parser.Int32()
                          .Choose(choices)
                          .Parse(input);
                Assert.That(success, Is.EqualTo(expectedSuccess));
                Assert.That(value, Is.EqualTo(expectedValue));
            }
        }

    }

    

}
