namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Services;

internal sealed class ServicesCommand : Command
{
    public ServicesCommand() : base("services")
    {
        Description = "Inspect telemetry services.";

        Subcommands.Add(new ListServicesCommand());
        Subcommands.Add(new ShowServiceCommand());
    }
}
