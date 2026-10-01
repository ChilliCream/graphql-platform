using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry;

internal static class TelemetryCommandOptions
{
    public static void AddOptions(Command command)
    {
        command.Options.Add(Opt<OptionalCloudUrlOption>.Instance);
        command.Options.Add(Opt<OptionalApiKeyOption>.Instance);
        command.Options.Add(Opt<OptionalOutputFormatOption>.Instance);
    }

    public static bool TryGetWorkspaceId(
        INitroConsole console,
        ParseResult parseResult,
        ISessionService sessionService,
        out string workspaceId)
    {
        try
        {
            parseResult.AssertHasAuthentication(sessionService);
            workspaceId = parseResult.GetWorkspaceId(sessionService);
            return true;
        }
        catch (ExitException exception)
        {
            workspaceId = string.Empty;
            var hint = sessionService.Session is null
                ? "run `nitro login`."
                : "run `nitro workspace set-default`.";
            TelemetryErrorRenderer.Render(console, exception.Message, hint);
            return false;
        }
    }
}
