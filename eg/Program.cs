namespace Arqs.Sample
{
    using System;

    static class Program
    {
        static int Main(string[] args) =>
            CommandLine.Run(args,

                from usg in Help.Text("usage: git commit [<options>] <pathspec>...", string.Empty)

                join h   in Arg.Flag  ("h|help                     print this summary") on 1 equals 1
                join q   in Arg.Flag  ("q|quiet                    suppress summary after successful commit") on 1 equals 1
                join v   in Arg.Flag  ("v|verbose                  show diff in commit message template") on 1 equals 1
                join dbg in Arg.Flag  ("d|debug                    debug program").Visibility(Visibility.Hidden) on 1 equals 1

                join cmo in Help.Text (string.Empty, "Commit message options") on 1 equals 1

                join f   in Arg.Option("F|file=<file>              read message from file") on 1 equals 1
                join a   in Arg.Option("author=<author>            override author for commit") on 1 equals 1
                join d   in Arg.Option("date=<date>                override date for commit", Parser.DateTime().Nullable()) on 1 equals 1
                join m   in Arg.Option("m|message=<message>        commit message") on 1 equals 1
                join rem in Arg.Option("c|reedit-message=<commit>  reuse and edit message from specified commit") on 1 equals 1
                join rum in Arg.Option("C|reuse-message=<commit>   reuse message from specified commit") on 1 equals 1
                join fx  in Arg.Option("fixup=<commit>             use autosquash formatted message to fixup specified commit") on 1 equals 1
                join sq  in Arg.Option("squash=<commit>            use autosquash formatted message to squash specified commit") on 1 equals 1
                join ra  in Arg.Flag  ("reset-author               the commit is authored by me now (used with -C/-c/--amend)") on 1 equals 1
                join so  in Arg.Flag  ("s|signoff                  add Signed-off-by:") on 1 equals 1
                join tf  in Arg.Option("t|template=<file>          use specified template file") on 1 equals 1
                join e   in Arg.Flag  ("e|edit                     force edit of commit") on 1 equals 1
                join cu  in Arg.Option("cleanup=<default>          how to strip spaces and #comments from message") on 1 equals 1
                join st  in Arg.Flag  ("status                     include status in commit message template") on 1 equals 1
                join gs  in Arg.Option("S|gpg-sign=<key-id>        include status in commit message template").DefaultValue() on 1 equals 1

                join cco in Help.Text (string.Empty, "Commit contents options") on 1 equals 1

                join all in Arg.Flag  ("a|all                      commit all changed files") on 1 equals 1
                join inc in Arg.Flag  ("i|include                  add specified files to index for commit") on 1 equals 1
                join ia  in Arg.Flag  ("interactive                interactively add files") on 1 equals 1
                join p   in Arg.Flag  ("p|patch                    interactively add changes") on 1 equals 1
                join o   in Arg.Flag  ("o|only                     commit only specified files") on 1 equals 1
                join n   in Arg.Flag  ("n|no-verify                bypass pre-commit and commit-msg hooks") on 1 equals 1
                join dr  in Arg.Flag  ("dry-run                    show what would be committed") on 1 equals 1
                join s   in Arg.Flag  ("short                      show status concisely") on 1 equals 1
                join b   in Arg.Flag  ("branch                     show branch information") on 1 equals 1
                join ab  in Arg.Flag  ("ahead-behind               compute full ahead / behind values") on 1 equals 1
                join por in Arg.Flag  ("porcelain                  machine - readable output") on 1 equals 1
                join l   in Arg.Flag  ("long                       show status in long format(default)") on 1 equals 1
                join z   in Arg.Flag  ("z|null                     terminate entries with NUL") on 1 equals 1
                join am  in Arg.Flag  ("amend                      amend previous commit") on 1 equals 1
                join npr in Arg.Flag  ("no-post-rewrite            bypass post-rewrite hook") on 1 equals 1
                join u   in Arg.Option("u|untracked-files[=<mode>] show untracked files, optional modes: all, normal, no. (Default: all)", "all").DefaultValue() on 1 equals 1

                select CommandLine.EntryPoint(h ? EntryPointMode.ShowHelp : EntryPointMode.RunMain, args =>
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
                    })));
    }
}
