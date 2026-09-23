using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;
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

        if (!TryCompileFilter(console, filterText, out var filter, out var parsedFilter))
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
        var emptyResultHint = await GetEmptyResultHintAsync(
            client,
            workspaceId,
            items,
            filterText,
            search,
            parsedFilter,
            since,
            until,
            cancellationToken);
        var renderer = new TelemetryListRenderer(console);
        renderer.Render(
            items,
            total: null,
            page.HasNextPage,
            ServiceListJsonContext.Default.ServiceListItem,
            emptyResultHint);

        return ExitCodes.Success;
    }

    private static async Task<string?> GetEmptyResultHintAsync(
        ITelemetryClient client,
        string workspaceId,
        IReadOnlyList<ServiceListItem> items,
        string? filterText,
        string? search,
        FilterNode? parsedFilter,
        DateTimeOffset? since,
        DateTimeOffset? until,
        CancellationToken cancellationToken)
    {
        if (items.Count != 0
            || (string.IsNullOrWhiteSpace(filterText) && string.IsNullOrWhiteSpace(search)))
        {
            return null;
        }

        try
        {
            var attributeKeys = await client.ListAttributeKeysAsync(
                workspaceId,
                OpenTelemetrySignalKind.Traces,
                kinds: null,
                search: null,
                since,
                until,
                first: 50,
                after: null,
                cancellationToken);
            return KeySuggestions.CreateHint(parsedFilter, attributeKeys.Items, OpenTelemetrySignalKind.Traces);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static bool TryCompileFilter(
        INitroConsole console,
        string? filterText,
        out OpenTelemetryFilterInput? filter,
        out FilterNode? parsedFilter)
    {
        try
        {
            filter = FilterFlags.Compile(
                filterText,
                TelemetryFilterSignal.Traces,
                hasError: false,
                minDurationMs: null,
                severity: null,
                traceId: null,
                search: null,
                service: null,
                out parsedFilter);
            return true;
        }
        catch (FilterParseException exception)
        {
            filter = null;
            parsedFilter = null;
            RenderFilterParseError(console, filterText!, exception);
            return false;
        }
    }

    private static void RenderFilterParseError(
        INitroConsole console,
        string filterText,
        FilterParseException exception)
    {
        console.Error.WriteErrorLine(
            $"filter: {exception.Message.EscapeMarkup()} at column {exception.Column}{Environment.NewLine}"
            + filterText.EscapeMarkup()
            + Environment.NewLine
            + new string(' ', exception.Column - 1)
            + "^"
            + Environment.NewLine
            + "hint: status:error http.status_code:>=500; -service.version:\"1.0.0\" duration:>=1000; @event.exception.type:\"TimeoutError\"");
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
