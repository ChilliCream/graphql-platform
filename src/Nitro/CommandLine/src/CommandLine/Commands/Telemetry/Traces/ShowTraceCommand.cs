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

        TelemetryCommandOptions.AddOptions(this);

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
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        var traceId = parseResult.GetRequiredValue(Opt<TraceIdArgument>.Instance);
        var spanId = parseResult.GetValue(Opt<TraceSpanOption>.Instance);
        var seeker = parseResult.GetSeeker();

        var trace = await client.GetTraceAsync(workspaceId, traceId, spanId, seeker, cancellationToken);

        if (trace is null || trace.Spans.Count == 0)
        {
            throw ThrowHelper.Exit($"The trace '{traceId.EscapeMarkup()}' was not found.");
        }

        if (!console.IsHumanReadable)
        {
            var detail = TraceJson.From(traceId, trace);
            resultHolder.SetResult(new ObjectResult(detail));
            return ExitCodes.Success;
        }

        var tree = SpanTreeBuilder.Build(trace.Spans);
        RenderSummary(console, traceId, trace);
        var renderedTree = new SpanTreeRenderer().Render(tree, spanId);
        if (renderedTree.Length > 0)
        {
            foreach (var line in renderedTree.Split(Environment.NewLine, StringSplitOptions.None))
            {
                console.WriteRawLine(line);
            }
        }

        return ExitCodes.Success;
    }

    private static void RenderSummary(INitroConsole console, string traceId, Trace trace)
    {
        var returnedSpanCount = trace.Spans.Count;
        if (trace.SpansTruncated)
        {
            var totalSpanCount = trace.SpanCount is { } count ? $"total {count} spans" : "total span count unknown";
            console.WriteRawLine(
                $"trace {traceId}: {returnedSpanCount} returned spans (errors unknown), "
                    + $"{totalSpanCount}, duration unknown (server-capped)");
        }
        else
        {
            var errorCount = trace.Spans.Count(static span => span.IsError);
            console.WriteRawLine(
                $"trace {traceId}: {returnedSpanCount} spans ({errorCount} errors), "
                    + $"total {trace.TotalDuration.FormatDuration()} ms");
        }

        var operations = trace
            .Spans.GroupBy(static span => span.SpanName, StringComparer.Ordinal)
            .Select(static group => new OperationSummary(
                group.Key,
                group.Count(),
                group.Average(static span => span.DurationMs),
                group.Select(static span => span.DurationMs).Percentile(0.95)))
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
                    $"  {operation.Name.EscapeControlCharacters()}: {operation.Count} spans, "
                        + $"avg {operation.AverageDurationMs.FormatDuration()} ms, "
                        + $"p95 {operation.P95DurationMs.FormatDuration()} ms");
            }
        }
    }

    private sealed record OperationSummary(string Name, int Count, double AverageDurationMs, double P95DurationMs);
}

file static class Extensions
{
    extension(ParseResult parseResult)
    {
        public string? GetSeeker()
        {
            if (parseResult.GetResult(Opt<TraceSeekerOption>.Instance) is { Implicit: false })
            {
                return parseResult.GetValue(Opt<TraceSeekerOption>.Instance);
            }

            return null;
        }
    }

    extension(IEnumerable<double> values)
    {
        public double Percentile(double percentile)
        {
            var sorted = values.OrderBy(static value => value).ToArray();
            if (sorted.Length == 0)
            {
                return 0;
            }

            var index = (int)Math.Ceiling(sorted.Length * percentile) - 1;
            return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
        }
    }
}
