namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Logs;

internal sealed class LogsCommand : Command
{
    public LogsCommand() : base("logs")
    {
        Description = "Inspect telemetry logs.";

        Subcommands.Add(new ListLogsCommand());
        Subcommands.Add(new ShowLogCommand());
    }
}
