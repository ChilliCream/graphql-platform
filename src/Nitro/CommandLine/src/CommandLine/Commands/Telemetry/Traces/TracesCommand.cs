namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces;

internal sealed class TracesCommand : Command
{
    public TracesCommand() : base("traces")
    {
        Description = "Inspect telemetry traces.";

        Subcommands.Add(new ListTraceCommand());
        Subcommands.Add(new ShowTraceCommand());
    }
}
