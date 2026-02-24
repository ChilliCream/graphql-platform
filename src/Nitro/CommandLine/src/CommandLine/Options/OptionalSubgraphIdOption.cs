namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalSubgraphIdOption : Option<string>
{
    public OptionalSubgraphIdOption() : base("--subgraph-id", "The id of the subgraph")
    {
        IsRequired = false;
        IsHidden = true;
        this.DefaultFromEnvironmentValue("SUBGRAPH_ID");
    }
}
