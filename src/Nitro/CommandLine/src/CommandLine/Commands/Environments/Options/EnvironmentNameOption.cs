namespace ChilliCream.Nitro.CommandLine.Commands.Environments.Options;

public sealed class EnvironmentNameOption : Option<string>
{
    public EnvironmentNameOption() : base(["--name", "-n"])
    {
        IsRequired = false;
        Description = "The name of the environment.";
    }
}
