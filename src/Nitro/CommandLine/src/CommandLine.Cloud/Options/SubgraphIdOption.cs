namespace ChilliCream.Nitro.CommandLine.Cloud.Option;

internal class SubgraphIdOption : Option<string>
{
    public SubgraphIdOption() : base("--subgraph-id", "The id of the subgraph")
    {
        IsRequired = true;
        this.DefaultFromEnvironmentValue("SUBGRAPH_ID");
    }
}

internal sealed class OptionalSubgraphIdOption : SubgraphIdOption
{
    public OptionalSubgraphIdOption() : base()
    {
        IsRequired = false;
    }
}
