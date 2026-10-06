using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
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
        Options.Add(Opt<OptionalCursorOption>.Instance);
        Options.Add(Opt<TelemetrySeverityOption>.Instance);
        Options.Add(Opt<TelemetryTraceIdOption>.Instance);
        Options.Add(Opt<TelemetryLogSearchOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        TelemetryCommandOptions.AddTimeRangeValidator(this);

        this.AddExamples(
            "telemetry logs list",
            "telemetry logs list --service checkout --severity error --since 2h",
            "telemetry logs list --filter \"severity:error\"");

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

        var filterText = parseResult.GetValue(Opt<TelemetryFilterOption>.Instance);
        var service = parseResult.GetValue(Opt<TelemetryServiceOption>.Instance);
        var severity = parseResult.GetValue(Opt<TelemetrySeverityOption>.Instance);
        var traceId = parseResult.GetValue(Opt<TelemetryTraceIdOption>.Instance);
        var search = parseResult.GetValue(Opt<TelemetryLogSearchOption>.Instance);

        if (!CompiledTelemetryFilter.TryCreate(
                console,
                filterText,
                severity?.ToString(),
                traceId,
                search,
                service,
                out var filter))
        {
            return ExitCodes.Error;
        }

        var environments = parseResult.GetValue(Opt<TelemetryEnvironmentOption>.Instance);
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var limit = parseResult.GetValue(Opt<TelemetryLimitOption>.Instance) ?? 50;

        var page = await client.ListLogsAsync(
            workspaceId,
            filter.Input,
            environments,
            since,
            until,
            limit,
            cursor,
            cancellationToken);

        var items = page.Items.OrderByDescending(static log => log.Start).Select(LogListItem.From).ToArray();
        resultHolder.SetResult(new PaginatedListResult<LogListItem>(items, page.EndCursor));

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
