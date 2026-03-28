namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class FusionEnvironmentOption : Option<string?>
{
    public FusionEnvironmentOption() : base("--environment")
    {
        Description = "The name of the environment used for value substitution in the schema-settings.json files.";

        Aliases.Add("--env");
        Aliases.Add("-e");
    }
}
