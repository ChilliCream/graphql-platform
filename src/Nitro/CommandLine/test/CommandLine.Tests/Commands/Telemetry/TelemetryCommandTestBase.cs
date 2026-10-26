namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry;

public abstract class TelemetryCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string WorkspaceId = "workspace-from-session";
}
