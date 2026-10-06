using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces;

internal sealed class ListTraceCommand : Command
{
    public ListTraceCommand() : base("list")
    {
        Description = "List telemetry traces in the current workspace.";

        Options.Add(Opt<TelemetryServiceOption>.Instance);
        Options.Add(Opt<TelemetryEnvironmentOption>.Instance);
        Options.Add(Opt<TelemetryFilterOption>.Instance);
        Options.Add(Opt<TelemetryHasErrorOption>.Instance);
        Options.Add(Opt<TelemetryMinDurationOption>.Instance);
        Options.Add(Opt<TelemetrySpanKindOption>.Instance);
        Options.Add(Opt<TelemetryTraceSearchOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);
        Options.Add(Opt<TelemetryLimitOption>.Instance);
        Options.Add(Opt<OptionalCursorOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        TelemetryCommandOptions.AddTimeRangeValidator(this);

        this.AddExamples("telemetry traces list", "telemetry traces list --filter \"http.response.status_code:>=500\"");

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
        var search = parseResult.GetValue(Opt<TelemetryTraceSearchOption>.Instance);
        var service = parseResult.GetValue(Opt<TelemetryServiceOption>.Instance);
        if (!CompiledTelemetryFilter.TryCreate(
                console,
                filterText,
                parseResult.GetValue(Opt<TelemetryHasErrorOption>.Instance),
                parseResult.GetValue(Opt<TelemetryMinDurationOption>.Instance),
                search,
                service,
                out var filter))
        {
            return ExitCodes.Error;
        }

        var environments = parseResult.GetValue(Opt<TelemetryEnvironmentOption>.Instance);
        var spanKinds = parseResult.GetValue(Opt<TelemetrySpanKindOption>.Instance).ToOpenTelemetrySpanKinds();
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var limit = parseResult.GetValue(Opt<TelemetryLimitOption>.Instance) ?? 20;

        var page = await client.ListTracesAsync(
            workspaceId,
            filter.Input,
            environments,
            spanKinds,
            since,
            until,
            limit,
            cursor,
            cancellationToken);

        var items = page.Items.OrderByDescending(static trace => trace.Start).Select(TraceListItem.From).ToArray();
        resultHolder.SetResult(new PaginatedListResult<TraceListItem>(items, page.EndCursor));

        return ExitCodes.Success;
    }

    internal sealed record TraceListItem(
        DateTimeOffset Start,
        string Service,
        string Name,
        double DurationMs,
        string Status,
        string TraceId,
        string SpanId,
        string Seeker)
    {
        public static TraceListItem From(TraceRow trace)
            => new(
                DateTimeOffset.FromUnixTimeMilliseconds((long)trace.Start),
                trace.ServiceName,
                trace.SpanName,
                trace.DurationMs,
                trace.StatusCode,
                trace.TraceId,
                trace.SpanId,
                trace.Seeker);
    }
}

file static class Extensions
{
    private static readonly OpenTelemetrySpanKind[] s_defaultSpanKinds =
    [
        OpenTelemetrySpanKind.Server,
        OpenTelemetrySpanKind.Consumer
    ];

    extension(TelemetrySpanKind[]? requestedSpanKinds)
    {
        public OpenTelemetrySpanKind[] ToOpenTelemetrySpanKinds()
            => requestedSpanKinds is { Length: > 0 }
                ? requestedSpanKinds.Select(static spanKind => spanKind.ToOpenTelemetrySpanKind()).ToArray()
                : s_defaultSpanKinds;
    }

    extension(TelemetrySpanKind spanKind)
    {
        public OpenTelemetrySpanKind ToOpenTelemetrySpanKind()
            => spanKind switch
            {
                TelemetrySpanKind.Server => OpenTelemetrySpanKind.Server,
                TelemetrySpanKind.Client => OpenTelemetrySpanKind.Client,
                TelemetrySpanKind.Producer => OpenTelemetrySpanKind.Producer,
                TelemetrySpanKind.Consumer => OpenTelemetrySpanKind.Consumer,
                TelemetrySpanKind.Internal => OpenTelemetrySpanKind.Internal,
                _ => OpenTelemetrySpanKind.Unspecified
            };
    }
}
