using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces.Tree;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Traces.Tree;

public sealed class SpanTreeRendererTests
{
    [Fact]
    public void Render_Should_WriteSemanticFieldsAndIdLast_When_SpanHasHttpMetadata()
    {
        // arrange
        var span = CreateSpan(
            "root-id",
            duration: 123.5,
            status: "ERROR",
            resourceAttributes: [new("service.name", "payments")],
            spanAttributes:
            [
                new("code.function", "HandlePayment"),
                new("code.filepath", "/src/Handler.cs"),
                new("code.lineno", "42")
            ],
            data: new HttpTraceSpanData(
                "1.1",
                "GET",
                "https",
                500,
                "https://example.test/orders/1?verbose=true",
                null));
        var tree = SpanTreeBuilder.Build([span]);
        var selection = SpanSelection.SelectOverview(tree);

        // act
        var rendered = new SpanTreeRenderer().Render(selection);

        // assert
        Assert.Equal(
            "root-id [GET /orders/1 · payments · 123.5ms · ERROR · HandlePayment · /src/Handler.cs:42 · root-id]",
            rendered);
        Assert.EndsWith("· root-id]", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Should_WriteExceptionAndEveryStackLine_When_ErrorEventExists()
    {
        // arrange
        var exception = new TraceEvent(
            "exception",
            0,
            [
                new("exception.type", "InvalidOperationException"),
                new("exception.message", "payment failed"),
                new("exception.stacktrace", "at A()\nat B()\nat C()")
            ]);
        var tree = SpanTreeBuilder.Build([CreateSpan("root", events: [exception])]);

        // act
        var rendered = new SpanTreeRenderer().Render(SpanSelection.SelectOverview(tree));

        // assert
        Assert.Contains("exception: InvalidOperationException: payment failed", rendered, StringComparison.Ordinal);
        Assert.Contains("at A()", rendered, StringComparison.Ordinal);
        Assert.Contains("at B()", rendered, StringComparison.Ordinal);
        Assert.Contains("at C()", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Should_UseDatabaseAndGraphQlOperationLabels_When_DataIsAvailable()
    {
        // arrange
        var database = CreateSpan(
            "db",
            data: new DatabaseTraceSpanData(null, null, null, "SELECT", null, "postgresql", null, null));
        var graphQl = CreateSpan(
            "graphql",
            data: new GraphQLOperationTraceSpanData(null, new TraceOperation(null, "query", "GetOrder")));
        var tree = SpanTreeBuilder.Build([database, graphQl]);

        // act
        var rendered = new SpanTreeRenderer().Render(SpanSelection.SelectOverview(tree));

        // assert
        Assert.Contains("db [postgresql SELECT ·  · 1ms · db]", rendered, StringComparison.Ordinal);
        Assert.Contains("graphql [GetOrder ·  · 1ms · graphql]", rendered, StringComparison.Ordinal);
    }

    private static TraceSpan CreateSpan(
        string id,
        double duration = 1,
        string status = "OK",
        IReadOnlyList<TelemetryAttribute>? resourceAttributes = null,
        IReadOnlyList<TelemetryAttribute>? spanAttributes = null,
        IReadOnlyList<TraceEvent>? events = null,
        TraceSpanData? data = null)
        => new(
            id,
            "",
            id,
            "SERVER",
            duration,
            0,
            status,
            string.Empty,
            resourceAttributes ?? [],
            spanAttributes ?? [],
            events ?? [],
            data);
}
