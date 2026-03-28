namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionMigrateTargetArgument : Argument<string>
{
    public const string SubgraphConfig = "subgraph-config";

    public FusionMigrateTargetArgument() : base("TARGET")
    {
        this.FromAmong(SubgraphConfig);
    }
}
