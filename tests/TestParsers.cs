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
    using System.Globalization;

    public class TestParsers
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
    }

    public class Int32
    {
        [TestCase("2", true, 2, Description = "Positive number")]
        [TestCase("-232", true, -232, Description = "Negative number")]
        [TestCase("2147483648", false, 0, Description = "Out of range")]
        [TestCase("3x", false, 0, Description = "Invalid number")]
        [TestCase("x3", false, 0, Description = "Invalid number")]
        [TestCase("+6", true, 6, Description = "Leading sign")]
        public void Input(string input, bool expectedSuccess, int expectedValue)
        {
            var (success, value) = Parser.Int32().Parse(input);
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [TestCase("8", true, 8, 0, 10, Description = "In range")]
        [TestCase("-22", true, -22, -100, -22, Description = "Upper range limit")]
        [TestCase("203", true, 203, 203, 204, Description = "Lower range limit")]
        [TestCase("15", false, 0, 13, 14, Description = "Out of range")]
        [TestCase("3", false, 0, 5, 0, Description = "Invalid range")]
        public void Range(string input, bool expectedSuccess, int expectedValue, int lowerLimit, int upperLimit)
        {
            var (success, value) = Parser.Int32().Range(lowerLimit, upperLimit).Parse(input);
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [TestCase("10,15,12", true, new[] { 10, 15, 12 }, ',', Description = "Comma separated")]
        [TestCase("11;22;33", true, new[] { 11, 22, 33 }, ';', Description = "Semicolon separated")]
        [TestCase("-1,-2,-3,-6", true, new[] { -1, -2, -3, -6 }, ',', Description = "Negative numbers")]
        [TestCase("20", true, new[] { 20 }, ',', Description = "One element")]
        [TestCase("220,", true, new[] { 220 }, ',', Description = "One element ending with separator")]
        [TestCase("10,15x,12", false, null, ',', Description = "Invalid element in array")]
        public void Delimiter(string input, bool expectedSuccess, int[] expectedValue, char delimiter)
        {
            var (success, value) = Parser.Int32().Delimited(delimiter).Parse(input);
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [TestCase("10", true, 16, NumberStyles.AllowHexSpecifier, Description = "Hex number")]
        [TestCase("5,123,232", true, 5123232, NumberStyles.AllowThousands, Description = "Thousands separator")]
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

}
