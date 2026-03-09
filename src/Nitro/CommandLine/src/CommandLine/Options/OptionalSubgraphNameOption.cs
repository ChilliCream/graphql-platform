namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class OptionalSubgraphNameOption : Option<string>
{
    public OptionalSubgraphNameOption() : base("--subgraph-name", "The name of the subgraph")
    {
        IsRequired = false;
        IsHidden = true;
        this.DefaultFromEnvironmentValue("SUBGRAPH_NAME");
    }
}
