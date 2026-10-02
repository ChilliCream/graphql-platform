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
        Options.Add(Opt<TelemetryLogSearchOption>.Instance);

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
        var search = parseResult.GetValue(Opt<TelemetryLogSearchOption>.Instance);
        if (!TelemetryListFilter.TryCompile(
            console,
            TelemetryFilterSignal.Logs,
            filterText,
            hasError: false,
            minDurationMs: null,
            severity?.ToString(),
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
        var emptyResultHint = await TelemetryListFilter.CreateEmptyResultHintAsync(
            client,
            workspaceId,
            TelemetryFilterSignal.Logs,
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
            LogListJsonContext.Default.LogListItem,
            emptyResultHint,
            [
                Opt<TelemetrySinceOption>.Instance,
                Opt<TelemetryServiceOption>.Instance,
                Opt<TelemetryFilterOption>.Instance
            ]);

        return ExitCodes.Success;
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
