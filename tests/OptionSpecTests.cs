namespace Arqs.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class OptionSpecTests
    {
        [TestCase("f", "f", null, null, true, false, false, null, null)]
        [TestCase("f|flag", "f", "flag", null, true, false, false, null, null)]
        [TestCase("f|flag|flg", "f", "flag", "flg", true, false, false, null, null)]

        [TestCase("f          description of flag", "f", null, null, true, false, false, null, "description of flag")]
        [TestCase("f|flag     description of flag", "f", "flag", null, true, false, false, null, "description of flag")]
        [TestCase("f|flag|flg description of flag", "f", "flag", "flg", true, false, false, null, "description of flag")]

        [TestCase("flag", null, "flag", null, true, false, false, null, null)]
        [TestCase("flag|flg", null, "flag", "flg", true, false, false, null, null)]

        [TestCase("flag     description of flag", null, "flag", null, true, false, false, null, "description of flag")]
        [TestCase("flag|flg description of flag", null, "flag", "flg", true, false, false, null, "description of flag")]

        [TestCase("f|[no-]flag", "f", "flag", null, true, true, false, null, null)]
        [TestCase("f|[no-]flag|flg", "f", "flag", "flg", true, true, false, null, null)]

        [TestCase("f|[no-]flag     description of flag", "f", "flag", null, true, true, false, null, "description of flag")]
        [TestCase("f|[no-]flag|flg description of flag", "f", "flag", "flg", true, true, false, null, "description of flag")]

        [TestCase("[no-]flag", null, "flag", null, true, true, false, null, null)]
        [TestCase("[no-]flag|flg", null, "flag", "flg", true, true, false, null, null)]

        [TestCase("[no-]flag     description of flag", null, "flag", null, true, true, false, null, "description of flag")]
        [TestCase("[no-]flag|flg description of flag", null, "flag", "flg", true, true, false, null, "description of flag")]

        [TestCase("o=           ", "o", null, null, false, false, false, null, null)]
        [TestCase("o|option=    ", "o", "option", null, false, false, false, null, null)]
        [TestCase("o|option|opt=", "o", "option", "opt", false, false, false, null, null)]

        [TestCase("o=VAL           ", "o", null, null, false, false, false, "VAL", null)]
        [TestCase("o|option=VAL    ", "o", "option", null, false, false, false, "VAL", null)]
        [TestCase("o|option|opt=VAL", "o", "option", "opt", false, false, false, "VAL", null)]

        [TestCase("o[=VAL]           ", "o", null, null, false, false, true, "VAL", null)]
        [TestCase("o|option[=VAL]    ", "o", "option", null, false, false, true, "VAL", null)]
        [TestCase("o|option|opt[=VAL]", "o", "option", "opt", false, false, true, "VAL", null)]

        [TestCase("o[=]           ", "o", null, null, false, false, true, null, null)]
        [TestCase("o|option[=]    ", "o", "option", null, false, false, true, null, null)]
        [TestCase("o|option|opt[=]", "o", "option", "opt", false, false, true, null, null)]

        [TestCase("o=            description of option", "o", null, null, false, false, false, null, "description of option")]
        [TestCase("o|option=     description of option", "o", "option", null, false, false, false, null, "description of option")]
        [TestCase("o|option|opt= description of option", "o", "option", "opt", false, false, false, null, "description of option")]

        [TestCase("o=VAL            description of option", "o", null, null, false, false, false, "VAL", "description of option")]
        [TestCase("o|option=VAL     description of option", "o", "option", null, false, false, false, "VAL", "description of option")]
        [TestCase("o|option|opt=VAL description of option", "o", "option", "opt", false, false, false, "VAL", "description of option")]

        [TestCase("o[=VAL]            description of option", "o", null, null, false, false, true, "VAL", "description of option")]
        [TestCase("o|option[=VAL]     description of option", "o", "option", null, false, false, true, "VAL", "description of option")]
        [TestCase("o|option|opt[=VAL] description of option", "o", "option", "opt", false, false, true, "VAL", "description of option")]

        [TestCase("o[=]            description of option", "o", null, null, false, false, true, null, "description of option")]
        [TestCase("o|option[=]     description of option", "o", "option", null, false, false, true, null, "description of option")]
        [TestCase("o|option|opt[=] description of option", "o", "option", "opt", false, false, true, null, "description of option")]

        [TestCase("option=    ", null, "option", null, false, false, false, null, null)]
        [TestCase("option|opt=", null, "option", "opt", false, false, false, null, null)]

        [TestCase("option=VAL    ", null, "option", null, false, false, false, "VAL", null)]
        [TestCase("option|opt=VAL", null, "option", "opt", false, false, false, "VAL", null)]

        [TestCase("option[=VAL]    ", null, "option", null, false, false, true, "VAL", null)]
        [TestCase("option|opt[=VAL]", null, "option", "opt", false, false, true, "VAL", null)]

        [TestCase("option[=]    ", null, "option", null, false, false, true, null, null)]
        [TestCase("option|opt[=]", null, "option", "opt", false, false, true, null, null)]

        [TestCase("option=     description of option", null, "option", null, false, false, false, null, "description of option")]
        [TestCase("option|opt= description of option", null, "option", "opt", false, false, false, null, "description of option")]

        [TestCase("option=VAL     description of option", null, "option", null, false, false, false, "VAL", "description of option")]
        [TestCase("option|opt=VAL description of option", null, "option", "opt", false, false, false, "VAL", "description of option")]

        [TestCase("option[=VAL]     description of option", null, "option", null, false, false, true, "VAL", "description of option")]
        [TestCase("option|opt[=VAL] description of option", null, "option", "opt", false, false, true, "VAL", "description of option")]

        [TestCase("option[=]     description of option", null, "option", null, false, false, true, null, "description of option")]
        [TestCase("option|opt[=] description of option", null, "option", "opt", false, false, true, null, "description of option")]

        public void Parse(string s, string shortName, string longName, string abbreviatedName,
                                    bool isFlag, bool isNegatable, bool isValueOptional, string valueName,
                                    string description)
        {
            var spec = OptionSpec.Parse(s);
            if (shortName?.Length > 0)
                Assert.That(spec.Names.ShortName, Is.EqualTo(ShortOptionName.Parse(shortName[0])));
            Assert.That(spec.Names.LongName, Is.EqualTo(longName));
            Assert.That(spec.Names.AbbreviatedName, Is.EqualTo(abbreviatedName));
            Assert.That(spec.IsFlag, Is.EqualTo(isFlag));
            Assert.That(spec.IsLongNameNegatable, Is.EqualTo(isNegatable));
            Assert.That(spec.IsValueOptional, Is.EqualTo(isValueOptional));
            Assert.That(spec.ValueName, Is.EqualTo(valueName));
            Assert.That(spec.Description, Is.EqualTo(description));
        }
    }
}
