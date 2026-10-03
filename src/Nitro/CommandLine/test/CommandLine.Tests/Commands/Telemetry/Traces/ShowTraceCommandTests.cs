using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Traces;

public sealed class ShowTraceCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    private const string TraceId = "trace-id";

    [Fact]
    public async Task Show_Should_RenderSummaryTopOperationsAndTree_When_TraceExists()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupGetTrace(CreateTrace());

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertSuccess(
            """
            trace trace-id: 2 spans (1 errors), total 25 ms
            top operations:
              child: 1 spans, avg 5 ms, p95 5 ms
              root: 1 spans, avg 20 ms, p95 20 ms
            root [SERVER · orders · 20ms · root]
            └─ child [SERVER · orders · 5ms · ERROR · child]
            """);
    }

    [Fact]
    public async Task Show_Should_PassHiddenSeekerAndSpan_When_OptionsAreSpecified()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupGetTrace(CreateTrace());

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "show",
            TraceId,
            "--span",
            "child",
            "--seeker",
            "opaque-cursor",
            "--since",
            "2h");

        // assert
        result.AssertSuccess();
        TelemetryClientMock.Verify(
            x => x.GetTraceAsync(WorkspaceId, TraceId, "child", "opaque-cursor", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Show_Should_NotDeriveSeekerFromTimeBounds_When_SinceAndUntilAreSpecified()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupGetTrace(CreateTrace());

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "show",
            TraceId,
            "--since",
            "2h",
            "--until",
            "1h");

        // assert
        result.AssertSuccess();
        TelemetryClientMock.Verify(
            x => x.GetTraceAsync(WorkspaceId, TraceId, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Show_Should_WriteTraceJson_When_OutputIsJson()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupSessionWithWorkspace();
        SetupGetTrace(new Trace(1, false, 12.5, [CreateSpan("span-id")]));

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertSuccess(
            """
            {
              "traceId": "trace-id",
              "spanCount": 1,
              "spansTruncated": false,
              "totalDurationMs": 12.5,
              "spans": [
                {
                  "spanId": "span-id",
                  "parentSpanId": "",
                  "spanName": "span-id",
                  "spanKind": "SERVER",
                  "durationMs": 1,
                  "start": 0,
                  "statusCode": "OK",
                  "statusMessage": "",
                  "resourceAttributes": [],
                  "spanAttributes": [],
                  "events": [],
                  "data": null
                }
              ]
            }
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Show_Should_ReturnError_When_TraceIsMissingOrHasNoSpans(bool hasEmptyTrace)
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupGetTrace(hasEmptyTrace ? new Trace(0, false, 0, []) : null);

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertError(
            """
            The trace 'trace-id' was not found.
            hint: run nitro telemetry traces list --since 2h
            """);
    }

    [Fact]
    public async Task Show_Should_LabelSummaryAsServerCapped_When_TraceIsTruncatedAndCountIsUnknown()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupGetTrace(new Trace(null, true, 0, [CreateSpan("root", duration: 20)]));

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertSuccess(
            """
            trace trace-id: 1 returned spans (errors unknown), total span count unknown, duration unknown (server-capped)
            top operations:
              root: 1 spans, avg 20 ms, p95 20 ms
            root [SERVER ·  · 20ms · root]
            """);
    }

    private void SetupGetTrace(Trace? trace)
    {
        TelemetryClientMock
            .Setup(x =>
                x.GetTraceAsync(
                    WorkspaceId,
                    TraceId,
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(trace);
    }

    private static Trace CreateTrace()
        => new(
            2,
            false,
            25,
            [
                CreateSpan("root", duration: 20, resourceAttributes: [new("service.name", "orders")]),
                CreateSpan(
                    "child",
                    parent: "root",
                    duration: 5,
                    status: "ERROR",
                    resourceAttributes: [new("service.name", "orders")])
            ]);

    private static TraceSpan CreateSpan(
        string id,
        string parent = "",
        double duration = 1,
        string status = "OK",
        IReadOnlyList<TelemetryAttribute>? resourceAttributes = null)
        => new(id, parent, id, "SERVER", duration, 0, status, string.Empty, resourceAttributes ?? [], [], [], null);
}
