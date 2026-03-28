namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalSubgraphNameOption : Option<string>
{
    public OptionalSubgraphNameOption() : base("--subgraph-name")
    {
        Description = "The name of the subgraph";
        Required = false;
        Hidden = true;
        this.DefaultFromEnvironmentValue("SUBGRAPH_NAME");
    }
}
