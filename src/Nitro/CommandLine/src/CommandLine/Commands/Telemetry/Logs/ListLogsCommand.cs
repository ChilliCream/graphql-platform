using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Logs;

internal sealed class ListLogsCommand : Command
{
    public ListLogsCommand() : base("list")
    {
        Description = "List telemetry logs in the current workspace.";

        Options.Add(Opt<TelemetryServiceOption>.Instance);
        Options.Add(Opt<TelemetryEnvironmentOption>.Instance);
        Options.Add(Opt<TelemetryFilterOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);
        Options.Add(Opt<TelemetryLimitOption>.Instance);
        Options.Add(Opt<TelemetrySeverityOption>.Instance);
        Options.Add(Opt<TelemetryTraceIdOption>.Instance);
        Options.Add(Opt<TelemetrySearchOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        this.AddExamples(
            "telemetry logs list",
            "telemetry logs list --service checkout --severity error --since 2h");

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

        var filterText = parseResult.GetValue(Opt<TelemetryFilterOption>.Instance);
        var service = parseResult.GetValue(Opt<TelemetryServiceOption>.Instance);
        var severity = parseResult.GetValue(Opt<TelemetrySeverityOption>.Instance);
        var traceId = parseResult.GetValue(Opt<TelemetryTraceIdOption>.Instance);
        var search = parseResult.GetValue(Opt<TelemetrySearchOption>.Instance);
        if (!TryCompileFilter(
            console,
            filterText,
            severity,
            traceId,
            search,
            service,
            out var filter,
            out var parsedFilter))
        {
            return ExitCodes.Error;
        }

        var environments = parseResult.GetValue(Opt<TelemetryEnvironmentOption>.Instance);
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);
        var limit = parseResult.GetValue(Opt<TelemetryLimitOption>.Instance) ?? 50;
        var page = await client.ListLogsAsync(
            workspaceId,
            filter,
            environments,
            since,
            until,
            limit,
            after: null,
            cancellationToken);

        var items = page.Items
            .OrderByDescending(static log => log.Start)
            .Select(LogListItem.From)
            .ToArray();
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
            LogListJsonContext.Default.LogListItem,
            emptyResultHint);

        return ExitCodes.Success;
    }

    private static async Task<string?> GetEmptyResultHintAsync(
        ITelemetryClient client,
        string workspaceId,
        IReadOnlyList<LogListItem> items,
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
                OpenTelemetrySignalKind.Logs,
                kinds: null,
                search: null,
                since,
                until,
                first: 50,
                after: null,
                cancellationToken);
            return KeySuggestions.CreateHint(parsedFilter, attributeKeys.Items, OpenTelemetrySignalKind.Logs);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static bool TryCompileFilter(
        INitroConsole console,
        string? filterText,
        TelemetrySeverity? severity,
        string? traceId,
        string? search,
        string? service,
        out OpenTelemetryFilterInput? filter,
        out FilterNode? parsedFilter)
    {
        try
        {
            filter = FilterFlags.Compile(
                filterText,
                TelemetryFilterSignal.Logs,
                hasError: false,
                minDurationMs: null,
                severity?.ToString(),
                traceId,
                search,
                service,
                out parsedFilter);
            return true;
        }
        catch (FilterParseException exception)
        {
            RenderFilterParseError(console, filterText!, exception);
            filter = null;
            parsedFilter = null;
            return false;
        }
    }

    private static void RenderFilterParseError(
        INitroConsole console,
        string filter,
        FilterParseException exception)
    {
        console.Error.Write(new Text($"filter: {exception.Message} at column {exception.Column}"));
        console.Error.WriteLine();
        console.Error.Write(new Text(filter));
        console.Error.WriteLine();
        console.Error.Write(new Text($"{new string(' ', exception.Column - 1)}^"));
        console.Error.WriteLine();
        console.Error.Write(
            new Text(
                "hint: examples: `severity:error`, `@resource.service.name:checkout`, "
                + "or `exception.type:TimeoutException`"));
        console.Error.WriteLine();
    }

    internal sealed record LogListItem(
        string Id,
        double Epoch,
        string SeverityText,
        int SeverityNumber,
        string ServiceName,
        string Body,
        string TraceId,
        string SpanId)
    {
        public static LogListItem From(LogRow log)
            => new(
                log.Id,
                log.Start,
                log.SeverityText,
                log.SeverityNumber,
                log.ServiceName,
                log.Body,
                log.TraceId,
                log.SpanId);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ListLogsCommand.LogListItem))]
internal partial class LogListJsonContext : JsonSerializerContext;
