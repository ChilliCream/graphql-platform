namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class WatchModeOption : Option<bool>
{
    public WatchModeOption() : base("--watch")
    {
        Arity = ArgumentArity.ZeroOrOne;
    }
}
