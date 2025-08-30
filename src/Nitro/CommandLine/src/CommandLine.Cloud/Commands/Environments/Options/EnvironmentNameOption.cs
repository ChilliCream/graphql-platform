namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Environment;

public sealed class EnvironmentNameOption : Option<string>
{
    public EnvironmentNameOption() : base(["--name", "-n"])
    {
        IsRequired = false;
        Description = "The name of the environment.";
    }
}
