namespace Largs
{
    public partial interface IArg {}
    public partial class Args { }
    public partial class Arg<T> { }
    public partial class ListArg<T> { }
    public partial class TailArg<T> { }
    public partial class ArgInfo {}
    public partial interface IReader {}
    public partial interface IReader<out T> {}
    public partial interface IArgBinder<out T> {}
    public partial class ArgBinder {}
    public partial class Reader<T> {}

    public partial struct ParseResult<T> {}
    public partial class ParseResult { }
    public partial interface IParser {}
    public partial interface IParser<T> {}
    public partial interface IParser<T, TOptions> {}
    public partial class Parser {}
}
