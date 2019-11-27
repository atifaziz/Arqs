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
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    public class Tests
    {
        [Test]
        public void Test1()
        {
            var args =
                from foo  in Args.Option("foo", -1, Parser.Int32()).List()
                join bar  in Args.Flag("bar")  on 1 equals 1
                join baz  in Args.Option("baz", Parser.Int32().Nullable())  on 1 equals 1
                join qux  in Args.Option("qux", "?", Parser.String()) on 1 equals 1
                join xs   in Args.Option("x", Parser.String()).List() on 1 equals 1
                join pos1 in Args.Arg("x", Parser.String()) on 1 equals 1
                join pos2 in Args.Arg("x", Parser.String()) on 1 equals 1
                select new { Foo = foo, Bar = bar, Baz = baz, Qux = qux, X = string.Join(",", xs), Pos1 = pos1, Pos2 = pos2 };

            var commandLine = "1 --bar --foo 4 2 hello --foo 2 -x one -hx two world -x three".Split();

            var help = Args.Flag("h");
            var version = Args.Flag("v");

            var (mode, result, tail) =
                ArgBinder.Bind(
                    1, help,
                    2, version,
                    3, args,
                    commandLine);

            Assert.That(mode, Is.EqualTo(3));

            Assert.That(result.Foo, Is.EqualTo(new[] { 4, 2 }));
            Assert.That(result.Bar, Is.True);
            Assert.That(result.Baz, Is.Null);
            Assert.That(result.Qux, Is.EqualTo("?"));
            Assert.That(result.X, Is.EqualTo("one,two,three"));
            Assert.That(result.Pos1, Is.EqualTo("1"));
            Assert.That(result.Pos2, Is.EqualTo("2"));
            Assert.That(tail, Is.EqualTo(new[] { "hello", "world" }));

            var infos = new Queue<IArg>(args.Inspect());
            Assert.That(infos.Dequeue().Name, Is.EqualTo("foo"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("bar"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("baz"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("qux"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("x"));
            Assert.That(infos.Dequeue().Name, Is.Null);
            Assert.That(infos.Dequeue().Name, Is.Null);

            (mode, result, tail) =
                ArgBinder.Bind(
                    1, help,
                    2, version,
                    3, args,
                    commandLine.Prepend("-h").Prepend("-v").ToArray());

            Assert.That(mode, Is.EqualTo(1));
            Assert.That(tail, Is.EqualTo(commandLine.Prepend("-v")));

            (mode, result, tail) =
                ArgBinder.Bind(
                    1, help,
                    2, version,
                    3, args,
                    commandLine.Prepend("-v").ToArray());

            Assert.That(mode, Is.EqualTo(2));
            Assert.That(tail, Is.EqualTo(commandLine));
        }

        [Test]
        public void Test2()
        {
            var args =
                from foo  in Args.Option("foo", -1, Parser.Int32()).List()
                join bar  in Args.Flag("bar") on 1 equals 1
                join baz  in Args.Option("baz", Parser.Int32().Nullable()) on 1 equals 1
                join qux  in Args.Option("qux", "?", Parser.String()) on 1 equals 1
                join xs   in Args.Option("x", Parser.String()).List() on 1 equals 1
                join pos1 in Args.Arg("x", Parser.String()) on 1 equals 1
                join pos2 in Args.Arg("x", Parser.String()) on 1 equals 1
                join rest in Args.Arg("rest", Parser.String()).Tail() on 1 equals 1
                select new { Foo = foo, Bar = bar, Baz = baz, Qux = qux, X = string.Join(",", xs), Pos1 = pos1, Pos2 = pos2, Rest = rest };

            var commandLine = "1 --bar --foo 4 2 --foo 2 -x one -x two -x three hello world".Split();
            var (result, tail) = args.Bind(commandLine);

            Assert.That(result.Foo, Is.EqualTo(new[] { 4, 2 }));
            Assert.That(result.Bar, Is.True);
            Assert.That(result.Baz, Is.Null);
            Assert.That(result.Qux, Is.EqualTo("?"));
            Assert.That(result.X, Is.EqualTo("one,two,three"));
            Assert.That(result.Pos1, Is.EqualTo("1"));
            Assert.That(result.Pos2, Is.EqualTo("2"));
            Assert.That(result.Rest, Is.EqualTo(new[] { "hello", "world" }));
            Assert.That(tail, Is.Empty);

            var infos = new Queue<IArg>(args.Inspect());
            Assert.That(infos.Dequeue().Name, Is.EqualTo("foo"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("bar"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("baz"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("qux"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("x"));
            Assert.That(infos.Dequeue().Name, Is.Null);
            Assert.That(infos.Dequeue().Name, Is.Null);
            Assert.That(infos.Dequeue().Name, Is.Null);
        }

        [Test]
        public void Test3()
        {
            var args =
                from foo  in Args.Option("foo", -1, Parser.Int32()).List()
                join bar  in Args.Flag("bar").FlagPresence() on 1 equals 1
                join baz  in Args.Option("baz", Parser.Int32()).FlagPresence() on 1 equals 1
                select new { Foo = foo, Bar = bar, Baz = baz };

            var commandLine = "--foo 4 --foo 2 --baz 42 --bar".Split();
            var (result, tail) = args.Bind(commandLine);

            Assert.That(result.Foo, Is.EqualTo(new[] { 4, 2 }));
            Assert.That(result.Bar, Is.EqualTo((true, true)));
            Assert.That(result.Baz, Is.EqualTo((true, 42)));
            Assert.That(tail, Is.Empty);

            var infos = new Queue<IArg>(args.Inspect());
            Assert.That(infos.Dequeue().Name, Is.EqualTo("foo"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("bar"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("baz"));
        }

        [Test]
        public void Test4()
        {
            var args =
                from a in Args.Flag("a")
                join b in Args.Flag("b")  on 1 equals 1
                join c in Args.Flag("c")  on 1 equals 1
                join d in Args.Option("d", Parser.Int32()) on 1 equals 1
                select new { A = a, B = b, C = c, D = d };

            var commandLine = "-acd 42".Split();
            var (result, tail) = args.Bind(commandLine);

            Assert.That(result.A, Is.True);
            Assert.That(result.B, Is.False);
            Assert.That(result.C, Is.True);
            Assert.That(result.D, Is.EqualTo(42));
            Assert.That(tail, Is.Empty);

            var infos = new Queue<IArg>(args.Inspect());
            Assert.That(infos.Dequeue().Name, Is.EqualTo("a"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("b"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("c"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("d"));
        }
    }
}
