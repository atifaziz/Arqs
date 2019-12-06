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
            Run(from h in Arg.Flag("help").ShortName('h').Description("print this summary")
                join q in Arg.Flag("quiet").ShortName('q').Description("suppress summary after successful commit") on 1 equals 1
                join v in Arg.Flag("verbose").ShortName('v').Description("show diff in commit message template") on 1 equals 1
                join f in Arg.Option("file", Parser.String()).ValueName("<file>").ShortName('F').Description("read message from file") on 1 equals 1
                join a in Arg.Option("author", Parser.String()).ValueName("<author>").Description("override author for commit") on 1 equals 1
                join d in Arg.Option("date", Parser.DateTime().Nullable()).ValueName("<date>").Description("override date for commit") on 1 equals 1
                join m in Arg.Option("message", Parser.String()).ShortName('m').ValueName("<message>").Description("commit message") on 1 equals 1
                join rem in Arg.Option("reedit-message", Parser.String()).ShortName('c').ValueName("<commit>").Description("reuse and edit message from specified commit") on 1 equals 1
                join rum in Arg.Option("reuse-message", Parser.String()).ShortName('C').ValueName("<commit>").Description("reuse message from specified commit") on 1 equals 1
                join fx in Arg.Option("fixup", Parser.String()).ValueName("<commit>").Description("use autosquash formatted message to fixup specified commit") on 1 equals 1
                join sq in Arg.Option("squash", Parser.String()).ValueName("<commit>").Description("use autosquash formatted message to squash specified commit") on 1 equals 1
                join ra in Arg.Flag("reset-author").Description("the commit is authored by me now (used with -C/-c/--amend)") on 1 equals 1
                join so in Arg.Flag("signoff").ShortName('s').Description("add Signed-off-by:") on 1 equals 1
                join tf in Arg.Option("template", Parser.String()).ShortName('t').ValueName("<file>").Description("use specified template file") on 1 equals 1
                join e in Arg.Flag("edit").ShortName('e').Description("force edit of commit") on 1 equals 1
                join cu in Arg.Option("cleanup", Parser.String()).ValueName("<default>").Description("how to strip spaces and #comments from message") on 1 equals 1
                join st in Arg.Flag("status").Description("include status in commit message template") on 1 equals 1
                join gs in Arg.Option("gpg-sign", Parser.String()).ShortName('S').ValueName("<key-id>").DefaultValue().Description("include status in commit message template") on 1 equals 1
                join all in Arg.Flag("all").ShortName('a').Description("commit all changed files") on 1 equals 1
                join inc in Arg.Flag("include").ShortName('i').Description("add specified files to index for commit") on 1 equals 1
                join ia in Arg.Flag("interactive").Description("interactively add files") on 1 equals 1
                join p in Arg.Flag("patch").ShortName('p').Description("interactively add changes") on 1 equals 1
                join o in Arg.Flag("only").ShortName('o').Description("commit only specified files") on 1 equals 1
                join n in Arg.Flag("no-verify").ShortName('n').Description("bypass pre-commit and commit-msg hooks") on 1 equals 1
                join dr in Arg.Flag("dry-run").Description("show what would be committed") on 1 equals 1
                join s in Arg.Flag("short").Description("show status concisely") on 1 equals 1
                join b in Arg.Flag("branch").Description("show branch information") on 1 equals 1
                join ab in Arg.Flag("ahead-behind").Description("compute full ahead / behind values") on 1 equals 1
                join por in Arg.Flag("porcelain").Description("machine - readable output") on 1 equals 1
                join l in Arg.Flag("long").Description("show status in long format(default)") on 1 equals 1
                join z in Arg.Flag("null").ShortName('z').Description("terminate entries with NUL") on 1 equals 1
                join am in Arg.Flag("amend").Description("amend previous commit") on 1 equals 1
                join npr in Arg.Flag("no-post-rewrite").Description("bypass post-rewrite hook") on 1 equals 1
                join u in Arg.Option("untracked-files", Parser.String()).ShortName('u').ValueName("<mode>").DefaultValue().Description("show untracked files, optional modes: all, normal, no. (Default: all)") on 1 equals 1
                select Foo(h ? CommandAction.Help : CommandAction.Run, args =>
                {
                    Console.WriteLine(new
                    {
                        Quiet = q,
                        Verbose = v,
                        File = f,
                        Author = a,
                        Date = d,
                        Message = m,
                        ReeditMessage = rem,
                        ReuseMessage = rum,
                        Fixup = fx,
                        Squash = sq,
                        ResetAuthor = ra,
                        SignOff = so,
                        Template = tf,
                        Edit = e,
                        CleanUp = cu,
                        Status = st,
                        GpgSign = gs ?? "(default)",
                        All = all,
                        Include = inc,
                        Interactive = ia,
                        Patch = p,
                        Only = o,
                        NoVerify = n,
                        Short = s,
                        Branch = b,
                        AheadBehind = ab,
                        Porcelain = por,
                        Long = l,
                        Null = z,
                        Amend = am,
                        NoPostRewrite = npr,
                        UntrackedFiles = u ?? "(default)",
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
                if (arg.ValueName() is string vn)
                {
                    var optional = arg.Info is OptionArgInfo info && info.IsValueOptional;
                    sb.Append(optional ? "[=" : " ")
                      .Append(vn)
                      .Append(optional ? "]" : string.Empty);
                }

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
