namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionMigrateTargetArgument : Argument<string>
{
    public const string ArgumentName = "TARGET";
    public const string SubgraphConfig = "subgraph-config";

    public FusionMigrateTargetArgument() : base(ArgumentName)
    {
        Description = "The migration target.";
        this.AcceptOnlyFromAmong(SubgraphConfig);
    }
}
