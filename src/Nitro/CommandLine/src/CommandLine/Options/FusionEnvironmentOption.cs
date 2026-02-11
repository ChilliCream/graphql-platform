namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class FusionEnvironmentOption : Option<string?>
{
    public FusionEnvironmentOption() : base("--environment")
    {
        AddAlias("--env");
        AddAlias("-e");
    }
}
