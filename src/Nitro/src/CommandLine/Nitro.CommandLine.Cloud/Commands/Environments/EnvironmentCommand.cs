namespace ChilliCream.Nitro.CLI.Commands.Environment;

internal sealed class EnvironmentCommand : Command
{
    public EnvironmentCommand() : base("environment")
    {
        Description = "Use this command to manage environments";

        AddCommand(new CreateEnvironmentCommand());
        AddCommand(new ListEnvironmentCommand());
        AddCommand(new ShowEnvironmentCommand());
    }
}
