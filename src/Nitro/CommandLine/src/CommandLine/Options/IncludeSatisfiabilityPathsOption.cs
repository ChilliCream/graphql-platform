namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class IncludeSatisfiabilityPathsOption : Option<bool?>
{
    public IncludeSatisfiabilityPathsOption()
        : base("--include-satisfiability-paths")
    {
        Description = "Include paths in satisfiability error messages";
    }
}
