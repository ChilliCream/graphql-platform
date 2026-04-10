namespace ChilliCream.Nitro.CommandLine;

internal sealed class WatchModeOption : Option<bool>
{
    public WatchModeOption() : base("--watch")
    {
        Description = "Watch for file changes and recompose automatically";
        Arity = ArgumentArity.ZeroOrOne;
    }
}
