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
            "root",
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

        // act
        var rendered = new SpanTreeRenderer().Render(tree, spanId: null);

        // assert
        rendered.MatchInlineSnapshot(
            "root-name [GET /orders/1 · payments · 123.5ms · ERROR · HandlePayment · /src/Handler.cs:42 · root]");
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
        var withoutStackTrace = new TraceEvent(
            "exception",
            0,
            [new("exception.type", "TimeoutException"), new("exception.message", "gateway timed out")]);
        var tree = SpanTreeBuilder.Build([CreateSpan("root", events: [exception, withoutStackTrace])]);

        // act
        var rendered = new SpanTreeRenderer().Render(tree, spanId: null);

        // assert
        rendered.MatchInlineSnapshot(
            """
            root-name [SERVER ·  · 1ms · ERROR · root]
              exception: InvalidOperationException: payment failed
                 at A()
                 at B()
                 at C()
              exception: TimeoutException: gateway timed out
            """);
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
            data: new GraphQLOperationTraceSpanData(null, new GraphQLTraceOperation(null, "query", "GetOrder")));
        var tree = SpanTreeBuilder.Build([database, graphQl]);

        // act
        var rendered = new SpanTreeRenderer().Render(tree, spanId: null);

        // assert
        rendered.MatchInlineSnapshot(
            """
            ├─ db-name [postgresql SELECT ·  · 1ms · db]
            └─ graphql-name [GetOrder ·  · 1ms · graphql]
            """);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("missing")]
    public void Render_Should_ReturnEmpty_When_NoSpanMatches(string? spanId)
    {
        // arrange
        var tree = SpanTreeBuilder.Build(spanId is null ? [] : [CreateSpan("root")]);

        // act
        var rendered = new SpanTreeRenderer().Render(tree, spanId);

        // assert
        rendered.MatchInlineSnapshot("");
    }

    [Fact]
    public void Render_Should_WriteOnlyFocusedSubtree_When_SpanIdMatches()
    {
        // arrange
        var tree = SpanTreeBuilder.Build([
            CreateSpan("root"),
            CreateSpan("focus", parent: "root"),
            CreateSpan("sibling", parent: "root"),
            CreateSpan("child-a", parent: "focus"),
            CreateSpan("child-b", parent: "focus"),
            CreateSpan("leaf", parent: "child-a")
        ]);

        // act
        var rendered = new SpanTreeRenderer().Render(tree, spanId: "focus");

        // assert
        rendered.MatchInlineSnapshot(
            """
            focus-name [SERVER ·  · 1ms · focus]
            ├─ child-a-name [SERVER ·  · 1ms · child-a]
            │  └─ leaf-name [SERVER ·  · 1ms · leaf]
            └─ child-b-name [SERVER ·  · 1ms · child-b]
            """);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("first")]
    public void Render_Should_WriteEachSpanOnce_When_ParentLinksFormACycle(string? spanId)
    {
        // arrange
        var tree = SpanTreeBuilder.Build([
            CreateSpan("first", parent: "second"),
            CreateSpan("second", parent: "first")
        ]);

        // act
        var rendered = new SpanTreeRenderer().Render(tree, spanId);

        // assert
        rendered.MatchInlineSnapshot(
            """
            first-name [SERVER ·  · 1ms · first]
            └─ second-name [SERVER ·  · 1ms · second]
            """);
    }

    [Fact]
    public void Render_Should_WriteEveryComponent_When_TraceContainsARootAndACycle()
    {
        // arrange
        var tree = SpanTreeBuilder.Build([
            CreateSpan("root"),
            CreateSpan("first", parent: "second"),
            CreateSpan("second", parent: "first")
        ]);

        // act
        var rendered = new SpanTreeRenderer().Render(tree, spanId: null);

        // assert
        rendered.MatchInlineSnapshot(
            """
            root-name [SERVER ·  · 1ms · root]
            first-name [SERVER ·  · 1ms · first]
            └─ second-name [SERVER ·  · 1ms · second]
            """);
    }

    [Fact]
    public void Render_Should_PreserveFullValues_When_LabelsAreLong()
    {
        // arrange
        var value = new string('a', 150);
        var tree = SpanTreeBuilder.Build([
            CreateSpan(
                value,
                resourceAttributes: [new("service.name", value)],
                spanAttributes: [new("code.function", value), new("code.filepath", value), new("code.lineno", "42")],
                data: new GraphQLOperationTraceSpanData(null, new GraphQLTraceOperation(null, "query", value)))
        ]);

        // act
        var rendered = new SpanTreeRenderer().Render(tree, spanId: null);

        // assert
        Assert.Equal($"{value}-name [{value} · {value} · 1ms · {value} · {value}:42 · {value}]", rendered);
    }

    private static TraceSpan CreateSpan(
        string id,
        double duration = 1,
        string status = "OK",
        IReadOnlyList<TelemetryAttribute>? resourceAttributes = null,
        IReadOnlyList<TelemetryAttribute>? spanAttributes = null,
        IReadOnlyList<TraceEvent>? events = null,
        TraceSpanData? data = null,
        string parent = "")
        => new(
            id,
            parent,
            $"{id}-name",
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
