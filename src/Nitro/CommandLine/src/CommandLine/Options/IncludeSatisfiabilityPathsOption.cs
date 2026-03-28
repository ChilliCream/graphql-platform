using static ChilliCream.Nitro.CommandLine.CommandLineResources;

namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class IncludeSatisfiabilityPathsOption : Option<bool?>
{
    public IncludeSatisfiabilityPathsOption()
        : base("--include-satisfiability-paths")
    {
        Description = ComposeCommand_IncludeSatisfiabilityPaths_Description;
    }
}
