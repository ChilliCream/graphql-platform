using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Arguments;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces.Tree;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces;

internal sealed class ShowTraceCommand : Command
{
    public ShowTraceCommand() : base("show")
    {
        Description = "Show a telemetry trace.";

        Arguments.Add(Opt<TraceIdArgument>.Instance);
        Options.Add(Opt<TraceSpanOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);
        Options.Add(Opt<TraceSeekerOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);
        Options.Add(Opt<OptionalCloudUrlOption>.Instance);
        Options.Add(Opt<OptionalApiKeyOption>.Instance);
        Options.Add(Opt<TelemetryOutputFormatOption>.Instance);

        this.AddExamples("telemetry traces show \"<trace-id>\"");

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

        ConfigureOutput(console, parseResult);

        if (console.OutputFormat is OutputFormat.Ndjson)
        {
            return TelemetryErrorRenderer.Render(
                console,
                "The traces show command does not support ndjson output.",
                "use --output json or omit --output.");
        }

        if (!TryGetWorkspaceId(console, parseResult, sessionService, out var workspaceId))
        {
            return ExitCodes.Error;
        }

        var traceId = parseResult.GetRequiredValue(Opt<TraceIdArgument>.Instance);
        var spanId = parseResult.GetValue(Opt<TraceSpanOption>.Instance);
        var seeker = GetSeeker(parseResult);
        var trace = await client.GetTraceAsync(
            workspaceId,
            traceId,
            spanId,
            seeker,
            cancellationToken);

        if (trace is null || trace.Spans.Count == 0)
        {
            return TelemetryErrorRenderer.Render(
                console,
                $"The trace '{traceId}' was not found.",
                "run nitro telemetry traces list --since 2h");
        }

        if (console.OutputFormat is OutputFormat.Json)
        {
            var detail = TraceJson.From(traceId, trace);
            console.WriteRawLine(JsonSerializer.Serialize(detail, TraceJsonContext.Default.TraceJson));
            return ExitCodes.Success;
        }

        var tree = SpanTreeBuilder.Build(trace.Spans);
        var selection = spanId is null
            ? SpanSelection.SelectOverview(tree)
            : SpanSelection.SelectFocused(tree, spanId);

        RenderSummary(console, traceId, trace, selection);
        var renderedTree = new SpanTreeRenderer().Render(selection);
        if (renderedTree.Length > 0)
        {
            foreach (var line in renderedTree.Split(Environment.NewLine, StringSplitOptions.None))
            {
                console.WriteRawLine(line);
            }
        }

        return ExitCodes.Success;
    }

    private static void ConfigureOutput(INitroConsole console, ParseResult parseResult)
    {
        if (parseResult.GetValue(Opt<TelemetryOutputFormatOption>.Instance) is { } output)
        {
            console.SetOutputFormat(output);
        }
    }

    private static bool TryGetWorkspaceId(
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

    private static string? GetSeeker(ParseResult parseResult)
    {
        if (parseResult.GetResult(Opt<TraceSeekerOption>.Instance) is { Implicit: false })
        {
            return parseResult.GetValue(Opt<TraceSeekerOption>.Instance);
        }

        return null;
    }

    private static void RenderSummary(
        INitroConsole console,
        string traceId,
        Trace trace,
        SpanSelectionResult selection)
    {
        var returnedSpanCount = selection.TotalSpanCount;
        if (trace.SpansTruncated)
        {
            var totalSpanCount = trace.SpanCount is { } count
                ? $"total {count} spans"
                : "total span count unknown";
            console.WriteRawLine(
                $"trace {traceId}: {returnedSpanCount} returned spans (errors unknown), "
                + $"{totalSpanCount}, duration unknown (server-capped)");
        }
        else
        {
            var errorCount = trace.Spans.Count(SpanSelection.IsError);
            console.WriteRawLine(
                $"trace {traceId}: {returnedSpanCount} spans ({errorCount} errors), "
                + $"total {FormatDuration(trace.TotalDuration)} ms");
        }

        if (selection.Count < returnedSpanCount)
        {
            console.WriteRawLine($"shows {selection.Count} of {returnedSpanCount} spans");
        }

        var operations = trace.Spans
            .GroupBy(static span => span.SpanName, StringComparer.Ordinal)
            .Select(static group => new OperationSummary(
                group.Key,
                group.Count(),
                group.Average(static span => span.DurationMs),
                Percentile(group.Select(static span => span.DurationMs), 0.95)))
            .Where(static operation => operation.AverageDurationMs >= 5)
            .OrderByDescending(static operation => operation.Count)
            .ThenBy(static operation => operation.Name, StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        if (operations.Length > 0)
        {
            console.WriteRawLine("top operations:");
            foreach (var operation in operations)
            {
                console.WriteRawLine(
                    $"  {Truncate(operation.Name)}: {operation.Count} spans, "
                    + $"avg {FormatDuration(operation.AverageDurationMs)} ms, "
                    + $"p95 {FormatDuration(operation.P95DurationMs)} ms");
            }
        }
    }

    private static double Percentile(IEnumerable<double> values, double percentile)
    {
        var sorted = values.OrderBy(static value => value).ToArray();
        if (sorted.Length == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(sorted.Length * percentile) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static string FormatDuration(double duration)
        => duration.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Truncate(string value)
        => value.Length <= 120
            ? value
            : string.Concat(value.AsSpan(0, 119), "…");

    private sealed record OperationSummary(
        string Name,
        int Count,
        double AverageDurationMs,
        double P95DurationMs);
}

internal sealed record TraceJson(
    string TraceId,
    int SpanCount,
    bool SpansTruncated,
    double TotalDurationMs,
    IReadOnlyList<TraceJsonSpan> Spans)
{
    public static TraceJson From(string traceId, Trace trace)
        => new(
            traceId,
            trace.SpanCount ?? trace.Spans.Count,
            trace.SpansTruncated,
            trace.TotalDuration,
            trace.Spans.Select(TraceJsonSpan.From).ToArray());
}

internal sealed record TraceJsonSpan(
    string SpanId,
    string ParentSpanId,
    string SpanName,
    string SpanKind,
    double DurationMs,
    double Start,
    string StatusCode,
    string StatusMessage,
    IReadOnlyList<TelemetryAttribute> ResourceAttributes,
    IReadOnlyList<TelemetryAttribute> SpanAttributes,
    IReadOnlyList<TraceEvent> Events,
    TraceJsonData? Data)
{
    public static TraceJsonSpan From(TraceSpan span)
        => new(
            span.SpanId,
            span.ParentSpanId,
            span.SpanName,
            span.SpanKind,
            span.DurationMs,
            span.Start,
            span.StatusCode,
            span.StatusMessage,
            span.ResourceAttributes,
            span.SpanAttributes,
            span.Events,
            TraceJsonData.From(span.Data));
}

internal sealed record TraceJsonData(
    string Kind,
    string? Flavor,
    string? Method,
    string? Scheme,
    int? StatusCode,
    string? Url,
    string? UserAgent,
    string? ConnectionString,
    string? Instance,
    string? Name,
    string? Operation,
    string? Statement,
    string? System,
    string? User,
    TraceDocument? Document,
    TraceOperation? GraphQLOperation,
    TraceSelection? Selection)
{
    public static TraceJsonData? From(TraceSpanData? data)
        => data switch
        {
            HttpTraceSpanData http => new(
                "http",
                http.Flavor,
                http.Method,
                http.Scheme,
                http.StatusCode,
                http.Url,
                http.UserAgent,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null),
            DatabaseTraceSpanData database => new(
                "database",
                null,
                null,
                null,
                null,
                database.Url,
                null,
                database.ConnectionString,
                database.Instance,
                database.Name,
                database.Operation,
                database.Statement,
                database.System,
                database.User,
                null,
                null,
                null),
            GraphQLOperationTraceSpanData graphQl => new(
                "graphql.operation",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                graphQl.Document,
                graphQl.Operation,
                null),
            GraphQLResolverTraceSpanData resolver => new(
                "graphql.resolver",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                resolver.Selection),
            _ => null
        };
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TraceJson))]
internal partial class TraceJsonContext : JsonSerializerContext;
