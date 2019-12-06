namespace Arqs.Tests
{
    using Arqs;
    using NUnit.Framework;

    [TestFixture]
    public class TestOptionNames
    {
        const string S = "s";
        const string A = "long";
        const string L = "long-name";
        const string X = null;

        [TestCase(S, X, X, S, X, X, 1)]
        [TestCase(X, S, X, S, X, X, 1)]
        [TestCase(X, X, S, S, X, X, 1)]

        [TestCase(L, X, X, X, L, X, 1)]
        [TestCase(X, L, X, X, L, X, 1)]
        [TestCase(X, X, L, X, L, X, 1)]

        [TestCase(A, X, X, X, A, X, 1)]
        [TestCase(X, A, X, X, A, X, 1)]
        [TestCase(X, X, A, X, A, X, 1)]

        [TestCase(S, A, X, S, A, X, 2)]
        [TestCase(A, S, X, S, A, X, 2)]
        [TestCase(S, L, X, S, L, X, 2)]
        [TestCase(L, S, X, S, L, X, 2)]

        [TestCase(A, L, X, X, L, A, 2)]
        [TestCase(L, A, X, X, L, A, 2)]
        [TestCase(A, S, X, S, A, X, 2)]
        [TestCase(S, A, X, S, A, X, 2)]

        [TestCase(L, S, X, S, L, X, 2)]
        [TestCase(S, L, X, S, L, X, 2)]
        [TestCase(L, A, X, X, L, A, 2)]
        [TestCase(A, L, X, X, L, A, 2)]

        [TestCase(X, S, A, S, A, X, 2)]
        [TestCase(X, A, S, S, A, X, 2)]
        [TestCase(X, S, L, S, L, X, 2)]
        [TestCase(X, L, S, S, L, X, 2)]

        [TestCase(X, A, L, X, L, A, 2)]
        [TestCase(X, L, A, X, L, A, 2)]
        [TestCase(X, A, S, S, A, X, 2)]
        [TestCase(X, S, A, S, A, X, 2)]

        [TestCase(X, L, S, S, L, X, 2)]
        [TestCase(X, S, L, S, L, X, 2)]
        [TestCase(X, L, A, X, L, A, 2)]
        [TestCase(X, A, L, X, L, A, 2)]

        [TestCase(L, S, A, S, L, A, 3)]
        [TestCase(L, A, S, S, L, A, 3)]

        [TestCase(S, L, A, S, L, A, 3)]
        [TestCase(S, A, L, S, L, A, 3)]

        [TestCase(S, L, A, S, L, A, 3)]
        [TestCase(S, A, L, S, L, A, 3)]

        public void Guess(string name1, string name2, string name3, string @short, string @long, string abbr, int count)
        {
            var names = OptionNames.Guess(name1, name2, name3);

            Assert.That(names.Count, Is.EqualTo(count));
            if (@short is string sn)
                Assert.That(names.ShortName, Is.EqualTo(ShortOptionName.Parse(sn[0])));
            if (@long is string ln)
                Assert.That(names.LongName, Is.EqualTo(ln));
            if (abbr is string an)
                Assert.That(names.AbbreviatedName, Is.EqualTo(an));
        }
    }
}
