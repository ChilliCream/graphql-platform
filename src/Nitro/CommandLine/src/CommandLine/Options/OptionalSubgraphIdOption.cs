namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalSubgraphIdOption : Option<string>
{
    public OptionalSubgraphIdOption() : base("--subgraph-id")
    {
        Description = "The ID of the subgraph";
        Required = false;
        Hidden = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.SubgraphId);
    }
}
