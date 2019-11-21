namespace Largs
{
    public partial class Arg { }
    public partial class Arg<T> { }
    public partial class ArgInfo {}
    public partial interface IArgSource {}
    public partial interface IReader {}
    public partial interface IArgBinder<out T> {}
    public partial class ArgBinder {}

    public partial struct ParseResult<T> {}
    public partial class ParseResult { }
    public partial interface IParser {}
    public partial interface IParser<T> {}
    public partial interface IParser<T, TOptions> {}
    public partial class Parser {}
}
