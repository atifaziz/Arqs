namespace Largs.Sample
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    static class Program
    {
        static void Main(string[] args)
        {
            var q =
                from help in Arg.Flag("help").WithShortName("h").WithOtherName("?").Break()
                from num in Arg.Value("num", 123, Parser.Int32())
                    .WithShortName("n")
                    .WithOtherName("number-number-number")
                    .WithDescription("an integer.")
                    .WithDescription("the quick brown fox jumps over the lazy dog. the quick brown fox jumps over the lazy dog.")
                from str in Arg.Value("string", "str", Parser.String()).WithDescription("the quick brown fox jumps over the lazy dog. the quick brown fox jumps over the lazy dog.")
                from force in Arg.Flag("force")
                select new
                {
                    Help = help,
                    Num = num,
                };

            var (options, tail) = q.Bind(args);

            Console.WriteLine(options);
            Console.WriteLine(string.Join("; ", tail));

            if (options.Help)
                Describe(q, Console.Out);
        }

        static void Describe<T>(IArgBinder<T> binder, TextWriter writer)
        {
            var infos = new List<ArgInfo>();
            binder.Inspect(infos);
            Describe(infos, writer);
        }

        static void Describe(IEnumerable<ArgInfo> args, TextWriter writer)
        {
            var sb = new StringBuilder();
            foreach (var arg in args)
                Describe(arg);

            void Describe(ArgInfo arg)
            {
                sb.Clear();
                sb.Append("  ");
                if (arg.ShortName is string sn)
                    sb.Append('-').Append(sn);
                else
                    sb.Append("    ");
                if (arg.Name is string n)
                {
                    if (arg.ShortName != null)
                        sb.Append(", ");
                    sb.Append("--").Append(n);
                }

                if (arg.OtherName is string on)
                    sb.Append(", --").Append(@on);

                if (!arg.IsFlag)
                    sb.Append("=VALUE");

                var written = sb.Length;

                if (arg.Description == null)
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
                foreach (var line in GetLines(arg.Description))
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
