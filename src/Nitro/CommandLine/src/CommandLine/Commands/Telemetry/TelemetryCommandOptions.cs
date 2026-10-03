using System.CommandLine.Parsing;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry;

internal static class TelemetryCommandOptions
{
    public static void AddOptions(Command command)
    {
        command.Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);
        command.Options.Add(Opt<OptionalCloudUrlOption>.Instance);
        command.Options.Add(Opt<OptionalApiKeyOption>.Instance);
        command.Options.Add(Opt<OptionalOutputFormatOption>.Instance);
    }

    /// <summary>
    /// Requires <c>--since</c> to be earlier than <c>--until</c>.
    /// </summary>
    public static void AddTimeRangeValidator(Command command)
    {
        command.Validators.Add(result =>
        {
            var defaultSince = TelemetryOptionDefaults.GetDefaultSince();
            var defaultUntil = TelemetryOptionDefaults.GetUtcNow();

            if (TryGetTimestamp(result, Opt<TelemetrySinceOption>.Instance, defaultSince, out var since)
                && TryGetTimestamp(result, Opt<TelemetryUntilOption>.Instance, defaultUntil, out var until)
                && since >= until)
            {
                result.AddError(
                    $"Option '{TelemetrySinceOption.OptionName}' must be earlier than '{TelemetryUntilOption.OptionName}'.");
            }
        });
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
            var hint = sessionService.Session is null ? "run `nitro login`." : "run `nitro workspace set-default`.";
            TelemetryErrorRenderer.Render(console, exception.Message, hint);
            return false;
        }
    }

    private static bool TryGetTimestamp(
        CommandResult result,
        Option<DateTimeOffset> option,
        DateTimeOffset defaultValue,
        out DateTimeOffset timestamp)
    {
        if (result.GetResult(option) is not { } optionResult)
        {
            timestamp = defaultValue;
            return true;
        }

        try
        {
            timestamp = optionResult.GetValueOrDefault<DateTimeOffset>();
            return true;
        }
        catch (InvalidOperationException)
        {
            // The option has already reported its own parse error.
            timestamp = default;
            return false;
        }
    }
}
