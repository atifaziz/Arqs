namespace Largs
{
    public partial class Arg { }
    public partial class Arg<T> { }
    public partial class ArgInfo {}
    public partial interface IArgSource {}
    public partial interface IArgBinder<out T> {}
    public partial class ArgBinder {}

    public partial interface IParser {}
    public partial interface IParser<out T> {}
    public partial interface IParser<out T, TOptions> {}
    public partial class Parser {}
}
