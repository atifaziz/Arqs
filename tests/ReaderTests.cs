namespace Arqs.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class ReaderTests
    {
        [Test]
        public void InitWithNull()
        {
            Assert.That(() => Reader.Read<object>(null), Throws.ArgumentNullException);
        }

        [Test]
        public void ReadEmpty()
        {
            var reader = Enumerable.Empty<int>().Read();
            Assert.That(() => reader.Read(), Throws.InvalidOperationException);
        }

        [Test]
        public void Unread()
        {
            using var reader = Enumerable.Empty<int>().Read();
            reader.Unread(3);
            reader.Unread(2);
            reader.Unread(1);
            Assert.That(reader.Read(), Is.EqualTo(1));
            Assert.That(reader.Read(), Is.EqualTo(2));
            Assert.That(reader.Read(), Is.EqualTo(3));
        }

        [Test]
        public void Read()
        {
            var words = "the quick brown fox jumps over the lazy dog".Split();
            using var ir = words.Read();
            var list = new List<KeyValuePair<int, string>>();
            while (ir.TryRead(out var item, out var index))
            {
                if (index % 2 == 0 && ir.HasMore())
                    ir.Unread(",");
                list.Add(KeyValuePair.Create(index, item));
            }
            Assert.That(list, Is.EqualTo(new[]
            {
                KeyValuePair.Create(0 , "the"  ),
                KeyValuePair.Create(1 , ","    ),
                KeyValuePair.Create(2 , "quick"),
                KeyValuePair.Create(3 , ","    ),
                KeyValuePair.Create(4 , "brown"),
                KeyValuePair.Create(5 , ","    ),
                KeyValuePair.Create(6 , "fox"  ),
                KeyValuePair.Create(7 , ","    ),
                KeyValuePair.Create(8 , "jumps"),
                KeyValuePair.Create(9 , ","    ),
                KeyValuePair.Create(10, "over" ),
                KeyValuePair.Create(11, ","    ),
                KeyValuePair.Create(12, "the"  ),
                KeyValuePair.Create(13, ","    ),
                KeyValuePair.Create(14, "lazy" ),
                KeyValuePair.Create(15, ","    ),
                KeyValuePair.Create(16, "dog"  ),
            }));
        }
    }
}
