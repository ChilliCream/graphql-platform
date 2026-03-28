namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalSubgraphIdOption : Option<string>
{
    public OptionalSubgraphIdOption() : base("--subgraph-id", "The ID of the subgraph")
    {
        Required = false;
        Hidden = true;
        this.DefaultFromEnvironmentValue("SUBGRAPH_ID");
    }
}
