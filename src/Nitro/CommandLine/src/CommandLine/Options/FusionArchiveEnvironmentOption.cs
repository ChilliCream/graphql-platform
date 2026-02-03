namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class FusionArchiveEnvironmentOption : Option<string?>
{
    public FusionArchiveEnvironmentOption() : base("--environment")
    {
        Description = "TODO";

        AddAlias("--env");
        AddAlias("-e");
    }
}
