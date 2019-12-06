namespace Arqs.Sample
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text;

    enum CommandAction { Run, Help }

    sealed class Command<T>
    {
        public Command(
            IArgBinder<T> binder,
            Func<T, CommandAction> f,
            string[] args,
            Func<T, ImmutableArray<string>, int> runner)
        {

        }
    }

    static class Command
    {
        public static Command<T> Create<T>(IArgBinder<T> binder,
            Func<T, CommandAction> f,
            string[] args,
            Func<T, ImmutableArray<string>, int> runner) =>
            new Command<T>(binder, f, args, runner);
    }

    static class Program
    {
        public static int Run<T>(
            IArgBinder<T> binder,
            Func<T, CommandAction> f,
            string[] args,
            Func<T, ImmutableArray<string>, int> runner)
        {
            var (result, tail) = binder.Bind(args);
            switch (f(result))
            {
                case CommandAction.Help:
                    Describe(binder, Console.Out);
                    return 0;
                default:
                    return runner(result, tail);
            }
        }

        public static int Run(
            IArgBinder<(CommandAction, Func<ImmutableArray<string>, int>)> binder,
            string[] args)
        {
            var ((action, f), tail) = binder.Bind(args);
            switch (action)
            {
                case CommandAction.Help:
                    Describe(binder, Console.Out);
                    return 0;
                default:
                    return f(tail);
            }
        }

        static (CommandAction, Func<ImmutableArray<string>, int>) Foo(CommandAction a, Func<ImmutableArray<string>, int> b) =>
            (a, b);

        static int Main(string[] args) =>
            Run(from h in Arg.Flag("h", (ShortOptionName)'h').Description("print this summary")
                join q in Arg.Flag("quiet", (ShortOptionName)'q').Description("suppress summary after successful commit") on 1 equals 1
                join v in Arg.Flag("verbose", (ShortOptionName)'v').Description("show diff in commit message template") on 1 equals 1
                join f in Arg.Option("file", (ShortOptionName)'F', Parser.String()).Description("read message from file") on 1 equals 1
                join a in Arg.Option("author", Parser.String()).Description("override author for commit") on 1 equals 1
                join d in Arg.Option("date", Parser.String()).Description("override date for commit") on 1 equals 1
                select Foo(h ? CommandAction.Help : CommandAction.Run, args =>
                {
                    Console.WriteLine(new
                    {
                        Quiet = q,
                        Verbose = v,
                        File = f,
                        Author = a,
                        Date = d,
                        Tail = $"[{string.Join("; ", args)}]",
                    });
                    return 0;
                }),
                args);

        static int Main1(string[] args) =>
            Run(from help in Arg.Flag("help").ShortName('h')/*.WithOtherName("?").Break()*/
                join num in Arg.Option("num", 123, Parser.Int32())
                        .ShortName('n')
                        .Description("an integer.")
                        .Description("the quick brown fox jumps over the lazy dog. the quick brown fox jumps over the lazy dog.") on 1 equals 1
                join str in Arg.Operand("string", "str", Parser.String()).Description("the quick brown fox jumps over the lazy dog. the quick brown fox jumps over the lazy dog.") on 1 equals 1
                join force in Arg.Flag("force") on 1 equals 1
                join tail in Arg.Operand("force", Parser.String()).Tail() on 1 equals 1
                select new
                {
                    Help = help,
                    Num = num,
                    Force = force,
                    Str = str,
                    Tail = $"[{string.Join("; ", tail)}]",
                },
                e => e.Help ? CommandAction.Help : CommandAction.Run,
                args,
                (e, _) =>
                {
                    Console.WriteLine(e);
                    return 0;
                });

        static void Describe<T>(IArgBinder<T> binder, TextWriter writer) =>
            Describe(binder.Inspect(), writer);

        static void Describe(IEnumerable<IArg> args, TextWriter writer)
        {
            var sb = new StringBuilder();
            foreach (var arg in args)
                Describe(arg);

            void Describe(IArg arg)
            {
                sb.Clear();
                sb.Append("  ");
                if (arg.ShortName() is ShortOptionName sn)
                    sb.Append('-').Append(sn);
                else
                    sb.Append("    ");
                if (arg.Name() is string n)
                {
                    if (arg.ShortName() != null)
                        sb.Append(", ");
                    sb.Append("--").Append(n);
                }
/*TODO
                if (arg.OtherName is string on)
                    sb.Append(", --").Append(@on);
*/
                if (!arg.IsFlag())
                    sb.Append("=VALUE");

                var written = sb.Length;

                if (arg.Description() == null)
                {
                    writer.WriteLine(sb);
                    return;
                }

                if (written < OptionWidth)
                    sb.Append(new string(' ', OptionWidth - written));
                else
                {
                    writer.WriteLine(sb);
                    sb.Clear();
                    sb.Append(new string(' ', OptionWidth));
                }

                var indent = false;
                var prefix = new string(' ', OptionWidth + 2);
                foreach (var line in GetLines(arg.Description()))
                {
                    if (indent)
                        sb.Append(prefix);
                    sb.Append(line);
                    writer.WriteLine(sb);
                    sb.Clear();
                    indent = true;
                }
            }
        }

        const int OptionWidth = 29;

        static IEnumerable<string> GetLines(string description, int width = 80)
        {
            if (string.IsNullOrEmpty(description))
            {
                yield return string.Empty;
                yield break;
            }

            var length = width - OptionWidth - 1;
            int start = 0, end;
            do
            {
                end = GetLineEnd(start, length, description);
                var c = description[end - 1];
                if (char.IsWhiteSpace(c))
                    --end;
                var writeContinuation = end != description.Length && !IsEolChar(c);
                var line = description.Substring(start, end - start) +
                        (writeContinuation ? "-" : "");
                yield return line;
                start = end;
                if (char.IsWhiteSpace(c))
                    ++start;
                length = width - OptionWidth - 2 - 1;
            }
            while (end < description.Length);

            static int GetLineEnd(int start, int length, string description)
            {
                var end = Math.Min(start + length, description.Length);
                var sep = -1;
                for (var i = start + 1; i < end; ++i)
                {
                    if (description[i] == '\n')
                        return i + 1;
                    if (IsEolChar(description[i]))
                        sep = i + 1;
                }
                if (sep == -1 || end == description.Length)
                    return end;
                return sep;
            }

            static bool IsEolChar(char c) => !char.IsLetterOrDigit(c);
        }
    }
}
