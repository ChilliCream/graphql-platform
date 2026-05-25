namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalSubgraphNameOption : Option<string>
{
    public OptionalSubgraphNameOption() : base("--subgraph-name")
    {
        Description = "The name of the subgraph";
        Required = false;
        Hidden = true;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.SubgraphName);
    }
}
