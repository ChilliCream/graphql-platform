namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Environment;

internal sealed class EnvironmentCommand : Command
{
    public EnvironmentCommand() : base("environment")
    {
        Description = "Manage environments";

        this.AddNitroCloudDefaultOptions();

        AddCommand(new CreateEnvironmentCommand());
        AddCommand(new ListEnvironmentCommand());
        AddCommand(new ShowEnvironmentCommand());
    }
}
