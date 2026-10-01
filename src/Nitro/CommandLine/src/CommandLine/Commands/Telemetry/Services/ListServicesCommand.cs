using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Services;

internal sealed class ListServicesCommand : Command
{
    public ListServicesCommand() : base("list")
    {
        Description = "List telemetry services in the current workspace.";

        Options.Add(Opt<TelemetrySearchOption>.Instance);
        Options.Add(Opt<TelemetryEnvironmentOption>.Instance);
        Options.Add(Opt<TelemetryFilterOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);
        Options.Add(Opt<TelemetryLimitOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        this.AddExamples("telemetry services list");

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

        if (!TelemetryCommandOptions.TryGetWorkspaceId(console, parseResult, sessionService, out var workspaceId))
        {
            return ExitCodes.Error;
        }

        var search = parseResult.GetValue(Opt<TelemetrySearchOption>.Instance);
        var filterText = parseResult.GetValue(Opt<TelemetryFilterOption>.Instance);
        var environments = parseResult.GetValue(Opt<TelemetryEnvironmentOption>.Instance);
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);
        var limit = parseResult.GetValue(Opt<TelemetryLimitOption>.Instance) ?? 50;

        if (!TelemetryListFilter.TryCompile(
            console,
            TelemetryFilterSignal.Traces,
            filterText,
            hasError: false,
            minDurationMs: null,
            severity: null,
            traceId: null,
            search: null,
            service: null,
            out var filter,
            out var parsedFilter))
        {
            return ExitCodes.Error;
        }

        var page = await client.ListServicesAsync(
            workspaceId,
            search,
            filter,
            environments,
            since,
            until,
            limit,
            after: null,
            cancellationToken);

        var items = page.Items.Select(ServiceListItem.From).ToArray();
        var emptyResultHint = await TelemetryListFilter.GetEmptyResultHintAsync(
            client,
            workspaceId,
            TelemetryFilterSignal.Traces,
            items.Length,
            filterText,
            search,
            parsedFilter,
            since,
            until,
            cancellationToken);
        console.WriteListEnvelope(
            items,
            total: null,
            page.HasNextPage,
            ServiceListJsonContext.Default.ServiceListItem,
            emptyResultHint);

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

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ListServicesCommand.ServiceListItem))]
internal partial class ServiceListJsonContext : JsonSerializerContext;
