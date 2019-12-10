namespace Arqs.Sample
{
    using System;

    static class Program
    {
        static int Main(string[] args) =>
            CommandLine.Run(args,
                from h in Arg.Flag("help").ShortName('h').Description("print this summary")
                join q in Arg.Flag("quiet").ShortName('q').Description("suppress summary after successful commit") on 1 equals 1
                join dbg in Arg.Flag("debug").ShortName('d').Description("debug program").Visibility(Visibility.Hidden) on 1 equals 1
                join cmo in Help.Text(string.Empty, "Commit message options") on 1 equals 1
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
                join cco in Help.Text(string.Empty, "Commit contents options") on 1 equals 1
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
                join u in Arg.Option("untracked-files", "all", Parser.String()).ShortName('u').ValueName("<mode>").DefaultValue().Description("show untracked files, optional modes: all, normal, no. (Default: all)") on 1 equals 1
                select CommandLine.EntryPoint(h ? EntryPointMode.ShowHelp : EntryPointMode.RunMain, args =>
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
                        GpgSign = gs,
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
                        UntrackedFiles = u,
                        Tail = $"[{string.Join("; ", args)}]",
                    });
                    return 0;
                }));
    }
}
