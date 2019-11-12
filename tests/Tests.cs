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

namespace Largs.Tests
{
    using NUnit.Framework;

    public class Tests
    {
        [Test]
        public void Test1()
        {
            var args =
                from foo in Arg.Require("foo", Parser.Int32())
                from bar in Arg.Optional("bar", 123, Parser.Int32())
                from baz in Arg.OptionalValue("baz", Parser.Int32())
                from qux in Arg.Optional("qux", "?", Parser.String())
                select new { Foo = foo, Bar = bar, Baz = baz, Qux = qux };

            var commandLine = "--foo 42".Split();
            var actual = args.Bind(commandLine);

            Assert.That(actual.Foo, Is.EqualTo(42));
            Assert.That(actual.Bar, Is.EqualTo(123));
            Assert.That(actual.Baz, Is.Null);
            Assert.That(actual.Qux, Is.EqualTo("?"));
        }
    }
}
