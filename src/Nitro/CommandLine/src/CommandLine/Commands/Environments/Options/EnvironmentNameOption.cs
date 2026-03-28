namespace ChilliCream.Nitro.CommandLine.Commands.Environments.Options;

public sealed class EnvironmentNameOption : Option<string>
{
    public EnvironmentNameOption() : base("--name")
    {
        Required = false;
        Description = "The name of the environment.";
        Aliases.Add("-n");
    }
}
