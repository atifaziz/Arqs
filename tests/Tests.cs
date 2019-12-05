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
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    public class Tests
    {
        [Test]
        public void Test1()
        {
            var args =
                from h    in Arg.Flag("h")
                join v    in Arg.Flag("V") on 1 equals 1
                join vl   in Arg.CountedFlag("verbose", (ShortOptionName)'v') on 1 equals 1
                join foo  in Arg.Option("foo", -1, Parser.Int32()).List() on 1 equals 1
                join bar  in Arg.Flag("bar")  on 1 equals 1
                join baz  in Arg.Option("baz", Parser.Int32().Nullable())  on 1 equals 1
                join qux  in Arg.Option("qux", "?", Parser.String()) on 1 equals 1
                join opt  in Arg.Option("opt", ShortOptionName.Parse('o'), "?", Parser.String()).WithIsValueOptional(true).List() on 1 equals 1
                join xs   in Arg.Option("x", Parser.String()).List() on 1 equals 1
                join @int in Arg.IntOpt("int") on 1 equals 1
                join pos1 in Arg.Operand("x", Parser.String()) on 1 equals 1
                join pos2 in Arg.Operand("x", Parser.String()) on 1 equals 1
                join flag in Arg.Flag("f").List() on 1 equals 1
                join m    in Arg.Macro("macro", s => "-v there".Split()) on 1 equals 1
                join page in Arg.Flag("page", (ShortOptionName)'p').Negatable(true).List() on 1 equals 1
                select new
                {
                    Verbosity = vl,
                    Foo = foo,
                    Bar = bar,
                    Baz = baz,
                    Qux = qux,
                    Opt = opt,
                    X = string.Join(",", xs),
                    Int = @int,
                    Pos1 = pos1,
                    Pos2 = pos2,
                    Flag = flag,
                    Macro = m,
                    Page = page,
                };

            var commandLine = @"
                1 --bar -v -v -v --foo 4 2 hello
                -ofoo -obar -o --opt=baz -vo -vovo
                @some_macro
                --foo 2 -x one -42 -x two - world -x three -xfour
                -f -f -ff -f+ -f- -f-f+ -f+f- -ff- -f+vf-
                -v- --verbose --verbose+ --verbose-
                -p --page -p+ -p- --no-page
                "
                    .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            var (result, tail) =
                ArgBinder.Bind(args, commandLine);

            Assert.That(result.Verbosity, Is.EqualTo(7));
            Assert.That(result.Foo, Is.EqualTo(new[] { 4, 2 }));
            Assert.That(result.Bar, Is.True);
            Assert.That(result.Baz, Is.Null);
            Assert.That(result.Qux, Is.EqualTo("?"));
            Assert.That(result.Opt, Is.EqualTo(new[] { "foo", "bar", "?", "baz", "?", "vo" }));
            Assert.That(result.X, Is.EqualTo("one,two,three,four"));
            Assert.That(result.Int, Is.EqualTo(42));
            Assert.That(result.Pos1, Is.EqualTo("1"));
            Assert.That(result.Pos2, Is.EqualTo("2"));
            Assert.That(result.Flag, Is.EqualTo(new[] { true, true, true, true, true, false, false, true, true, false, true, false, true, false }));
            Assert.That(result.Macro.Name, Is.EqualTo("some_macro"));
            Assert.That(result.Macro.Args, Is.EqualTo(new[] { "-v", "there" }));
            Assert.That(tail, Is.EqualTo(new[] { "hello", "there", "-", "world" }));
            Assert.That(result.Page, Is.EqualTo(new[] { true, true, true, false, false }));

            var infos = new Queue<IArg>(args.Inspect());
            Assert.That(infos.Dequeue().ShortName().ToString(), Is.EqualTo("h"));
            Assert.That(infos.Dequeue().ShortName().ToString(), Is.EqualTo("V"));
            Assert.That(infos.Dequeue().ShortName().ToString(), Is.EqualTo("v"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("foo"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("bar"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("baz"));
            Assert.That(infos.Dequeue().Name, Is.EqualTo("qux"));
            Assert.That(infos.Dequeue().Name(), Is.EqualTo("opt"));
            Assert.That(infos.Dequeue().ShortName().ToString(), Is.EqualTo("x"));
            Assert.That(infos.Dequeue().Name, Is.Null);
            Assert.That(infos.Dequeue().Name, Is.Null);
            Assert.That(infos.Dequeue().Name, Is.Null);
            Assert.That(infos.Dequeue().ShortName().ToString(), Is.EqualTo("f"));
            Assert.That(((MacroArgInfo)infos.Dequeue().Info).ValueName, Is.EqualTo("macro"));
        }

        [Test]
        public void Test2()
        {
            var args =
                from foo  in Arg.Option("foo", -1, Parser.Int32()).List()
                join bar  in Arg.Flag("bar") on 1 equals 1
                join baz  in Arg.Option("baz", Parser.Int32().Nullable()) on 1 equals 1
                join qux  in Arg.Option("qux", "?", Parser.String()) on 1 equals 1
                join xs   in Arg.Option("x", Parser.String()).List() on 1 equals 1
                join pos1 in Arg.Operand("x", Parser.String()) on 1 equals 1
                join pos2 in Arg.Operand("x", Parser.String()) on 1 equals 1
                join rest in Arg.Operand("rest", Parser.String()).Tail() on 1 equals 1
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
            Assert.That(infos.Dequeue().ShortName().ToString(), Is.EqualTo("x"));
            Assert.That(infos.Dequeue().Name, Is.Null);
            Assert.That(infos.Dequeue().Name, Is.Null);
            Assert.That(infos.Dequeue().Name, Is.Null);
        }

        [Test]
        public void Test3()
        {
            var args =
                from foo in Arg.Flag("foo").FlagPresence()
                join bar in Arg.Option("bar", -1, Parser.Int32()).List() on 1 equals 1
                join baz in Arg.Option("baz", Parser.Int32()).FlagPresence() on 1 equals 1
                join qux in Arg.Option("qux", "default", Parser.String()).FlagPresence() on 1 equals 1
                select new { Foo = foo, Bar = bar, Baz = baz, Qux = qux };

            var commandLine = "--bar 4 --bar 2 --baz 42 --qux quux".Split();
            var (result, tail) = args.Bind(commandLine);

            Assert.That(result.Foo, Is.EqualTo((false, false)));
            Assert.That(result.Bar, Is.EqualTo(new[] { 4, 2 }));
            Assert.That(result.Baz, Is.EqualTo((true, 42)));
            Assert.That(result.Qux, Is.EqualTo((true, "quux")));
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
                from a in Arg.Flag("a")
                join b in Arg.Flag("b")  on 1 equals 1
                join c in Arg.Flag("c")  on 1 equals 1
                join d in Arg.Option("d", Parser.Int32()) on 1 equals 1
                select new { A = a, B = b, C = c, D = d };

            var commandLine = "-acd 42".Split();
            var (result, tail) = args.Bind(commandLine);

            Assert.That(result.A, Is.True);
            Assert.That(result.B, Is.False);
            Assert.That(result.C, Is.True);
            Assert.That(result.D, Is.EqualTo(42));
            Assert.That(tail, Is.Empty);

            var infos = new Queue<IArg>(args.Inspect());
            Assert.That(infos.Dequeue().ShortName().ToString(), Is.EqualTo("a"));
            Assert.That(infos.Dequeue().ShortName().ToString(), Is.EqualTo("b"));
            Assert.That(infos.Dequeue().ShortName().ToString(), Is.EqualTo("c"));
            Assert.That(infos.Dequeue().ShortName().ToString(), Is.EqualTo("d"));
        }
    }
}
