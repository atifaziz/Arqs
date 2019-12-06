namespace Arqs.Sample
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text;

    enum CommandAction { Run, Help }

    static class Program
    {
        public static int Run<T>(
            IArgBinder<T> binder,
            Func<T, CommandAction> f,
            Func<T, ImmutableArray<string>, int> runner,
            params string[] args)
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

        static int Main(string[] args)
        {
            var q =
                from help in Arg.Flag("help").ShortName('h')/*.WithOtherName("?").Break()*/
                join num in Arg.Option("num", 123, Parser.Int32())
                    .ShortName('n')
                    .Description("an integer.")
                    .Description("the quick brown fox jumps over the lazy dog. the quick brown fox jumps over the lazy dog.")
                    on 1 equals 1
                join str in Arg.Operand("string", "str", Parser.String()).Description("the quick brown fox jumps over the lazy dog. the quick brown fox jumps over the lazy dog.")
                    on 1 equals 1
                join force in Arg.Flag("force")
                    on 1 equals 1
                select new
                {
                    Help = help,
                    Num = num,
                    Force = force,
                    Str = str,
                };

            return
                Run(q, e => e.Help ? CommandAction.Help : CommandAction.Run, args: args, runner:
                (e, tail) =>
                {
                    Console.WriteLine(e);
                    Console.WriteLine(string.Join("; ", tail));
                    return 0;
                });
        }

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
