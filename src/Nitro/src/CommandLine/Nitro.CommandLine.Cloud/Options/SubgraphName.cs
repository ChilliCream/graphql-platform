namespace ChilliCream.Nitro.CLI.Option;

internal class SubgraphNameOption : Option<string>
{
    public SubgraphNameOption() : base("--subgraph-name", "The name of the subgraph")
    {
        IsRequired = true;
        this.DefaultFromEnvironmentValue("SUBGRAPH_NAME");
    }
}

internal sealed class OptionalSubgraphNameOption : SubgraphNameOption
{
    public OptionalSubgraphNameOption() : base()
    {
        IsRequired = false;
    }
}

