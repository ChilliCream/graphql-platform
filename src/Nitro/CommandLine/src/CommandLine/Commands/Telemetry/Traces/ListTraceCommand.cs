using System.Globalization;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces;

internal sealed class ListTraceCommand : Command
{
    private static readonly OpenTelemetrySpanKind[] s_defaultSpanKinds =
    [
        OpenTelemetrySpanKind.Server,
        OpenTelemetrySpanKind.Consumer
    ];

    public ListTraceCommand() : base("list")
    {
        Description = "List telemetry traces in the current workspace.";

        Options.Add(Opt<TelemetryServiceOption>.Instance);
        Options.Add(Opt<TelemetryEnvironmentOption>.Instance);
        Options.Add(Opt<TelemetryFilterOption>.Instance);
        Options.Add(Opt<TelemetryHasErrorOption>.Instance);
        Options.Add(Opt<TelemetryMinDurationOption>.Instance);
        Options.Add(Opt<TelemetrySpanKindOption>.Instance);
        Options.Add(Opt<TelemetrySearchOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);
        Options.Add(Opt<TelemetryLimitOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        this.AddExamples("telemetry traces list");

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

        TelemetryCommandOptions.ConfigureOutput(console, parseResult);

        if (!TelemetryCommandOptions.TryGetWorkspaceId(console, parseResult, sessionService, out var workspaceId))
        {
            return ExitCodes.Error;
        }

        var filterText = parseResult.GetValue(Opt<TelemetryFilterOption>.Instance);
        if (!TryCompileFilter(
            console,
            filterText,
            parseResult.GetValue(Opt<TelemetryHasErrorOption>.Instance),
            parseResult.GetValue(Opt<TelemetryMinDurationOption>.Instance),
            parseResult.GetValue(Opt<TelemetrySearchOption>.Instance),
            parseResult.GetValue(Opt<TelemetryServiceOption>.Instance),
            out var filter))
        {
            return ExitCodes.Error;
        }

        var environments = parseResult.GetValue(Opt<TelemetryEnvironmentOption>.Instance);
        var requestedSpanKinds = parseResult.GetValue(Opt<TelemetrySpanKindOption>.Instance);
        var spanKinds = requestedSpanKinds is { Length: > 0 }
            ? requestedSpanKinds.Select(MapSpanKind).ToArray()
            : s_defaultSpanKinds;
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);
        var limit = parseResult.GetValue(Opt<TelemetryLimitOption>.Instance) ?? 20;

        var page = await client.ListTracesAsync(
            workspaceId,
            filter,
            environments,
            spanKinds,
            since,
            until,
            limit,
            after: null,
            cancellationToken);

        var items = page.Items
            .OrderByDescending(static trace => trace.Start)
            .Select(TraceListItem.From)
            .ToArray();
        var renderer = new TelemetryListRenderer(console);
        renderer.Render(
            items,
            total: null,
            page.HasNextPage,
            "traces",
            TraceListJsonContext.Default.TraceListItem,
            new TelemetryListColumn<TraceListItem>("Start", item => FormatStart(item.Start)),
            new TelemetryListColumn<TraceListItem>("Service", item => item.Service),
            new TelemetryListColumn<TraceListItem>("Name", item => item.Name),
            new TelemetryListColumn<TraceListItem>("Duration (ms)", item => FormatDuration(item.DurationMs)),
            new TelemetryListColumn<TraceListItem>("Status", item => item.Status),
            new TelemetryListColumn<TraceListItem>("Trace ID", item => item.TraceId));

        return ExitCodes.Success;
    }

    private static bool TryCompileFilter(
        INitroConsole console,
        string? filterText,
        bool hasError,
        int? minDurationMs,
        string? search,
        string? service,
        out OpenTelemetryFilterInput? filter)
    {
        try
        {
            filter = FilterFlags.Compile(
                filterText,
                TelemetryFilterSignal.Traces,
                hasError,
                minDurationMs,
                severity: null,
                traceId: null,
                search,
                service);
            return true;
        }
        catch (FilterParseException exception)
        {
            RenderFilterParseError(console, filterText!, exception);
            filter = null;
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
                "hint: examples: `status:error`, `duration:>=100`, "
                + "or `@resource.service.name:checkout`"));
        console.Error.WriteLine();
    }

    private static string FormatStart(DateTimeOffset start)
        => start.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    private static string FormatDuration(double durationMs)
        => durationMs.ToString("0.###", CultureInfo.InvariantCulture);

    private static OpenTelemetrySpanKind MapSpanKind(TelemetrySpanKind spanKind)
        => spanKind switch
        {
            TelemetrySpanKind.Server => OpenTelemetrySpanKind.Server,
            TelemetrySpanKind.Client => OpenTelemetrySpanKind.Client,
            TelemetrySpanKind.Producer => OpenTelemetrySpanKind.Producer,
            TelemetrySpanKind.Consumer => OpenTelemetrySpanKind.Consumer,
            TelemetrySpanKind.Internal => OpenTelemetrySpanKind.Internal,
            _ => OpenTelemetrySpanKind.Unspecified
        };

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

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ListTraceCommand.TraceListItem))]
internal partial class TraceListJsonContext : JsonSerializerContext;
