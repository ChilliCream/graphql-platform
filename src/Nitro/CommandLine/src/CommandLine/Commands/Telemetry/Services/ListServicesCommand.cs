using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Services;

internal sealed class ListServicesCommand : Command
{
    public ListServicesCommand() : base("list")
    {
        Description = "List telemetry services in the current workspace.";

        Options.Add(Opt<TelemetryServiceSearchOption>.Instance);
        Options.Add(Opt<TelemetryEnvironmentOption>.Instance);
        Options.Add(Opt<TelemetryFilterOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);
        Options.Add(Opt<TelemetryLimitOption>.Instance);
        Options.Add(Opt<OptionalCursorOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        TelemetryCommandOptions.AddTimeRangeValidator(this);

        this.AddExamples("telemetry services list", "telemetry services list --filter \"status:error\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<ITelemetryClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        var search = parseResult.GetValue(Opt<TelemetryServiceSearchOption>.Instance);
        var filterText = parseResult.GetValue(Opt<TelemetryFilterOption>.Instance);
        var environments = parseResult.GetValue(Opt<TelemetryEnvironmentOption>.Instance);
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var limit = parseResult.GetValue(Opt<TelemetryLimitOption>.Instance) ?? 50;

        if (!CompiledTelemetryFilter.TryCreate(console, TelemetryFilterSignal.Traces, filterText, out var filter))
        {
            return ExitCodes.Error;
        }

        var page = await client.ListServicesAsync(
            workspaceId,
            search,
            filter.Input,
            environments,
            since,
            until,
            limit,
            cursor,
            cancellationToken);

        var items = page.Items.Select(ServiceListItem.From).ToArray();
        resultHolder.SetResult(new PaginatedListResult<ServiceListItem>(items, page.EndCursor));

        return ExitCodes.Success;
    }

    internal sealed record ServiceListItem(string Name, string Environments, string? LastVersion)
    {
        public static ServiceListItem From(ServiceRow service)
            => new(
                service.Name,
                string.Join(", ", service.EnvironmentNames),
                service.VersionMarkers.MaxBy(static marker => marker.FirstSeenAt)?.Version);
    }
}
