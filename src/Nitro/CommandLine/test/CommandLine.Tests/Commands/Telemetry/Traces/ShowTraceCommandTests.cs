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

    [Fact]
    public async Task Show_Should_WriteTypedSpanData_When_OutputIsJsonAndSpansCarryData()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupSessionWithWorkspace();
        SetupGetTrace(
            new Trace(
                4,
                false,
                10,
                [
                    CreateSpan(
                        "http-span",
                        data: new HttpTraceSpanData(
                            "1.1",
                            "GET",
                            "https",
                            200,
                            "https://example.com/products",
                            "test-agent/1.0")),
                    CreateSpan(
                        "database-span",
                        data: new DatabaseTraceSpanData(
                            "Host=localhost",
                            "primary",
                            "orders",
                            "SELECT",
                            "SELECT * FROM orders",
                            "postgresql",
                            "postgresql://localhost/orders",
                            "app")),
                    CreateSpan(
                        "operation-span",
                        data: new GraphQLOperationTraceSpanData(
                            new TraceDocument("query GetOrders { orders { id } }", "document-id"),
                            new TraceOperation("operation-hash", "query", "GetOrders"))),
                    CreateSpan(
                        "resolver-span",
                        data: new GraphQLResolverTraceSpanData(
                            new TraceSelection(
                                new TraceField("Query.orders", "Query", "orders"),
                                "orders",
                                "orders",
                                "[Order!]!")))
                ]));

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertSuccess(
            """
            {
              "traceId": "trace-id",
              "spanCount": 4,
              "spansTruncated": false,
              "totalDurationMs": 10,
              "spans": [
                {
                  "spanId": "http-span",
                  "parentSpanId": "",
                  "spanName": "http-span",
                  "spanKind": "SERVER",
                  "durationMs": 1,
                  "start": 0,
                  "statusCode": "OK",
                  "statusMessage": "",
                  "resourceAttributes": [],
                  "spanAttributes": [],
                  "events": [],
                  "data": {
                    "kind": "http",
                    "flavor": "1.1",
                    "method": "GET",
                    "scheme": "https",
                    "statusCode": 200,
                    "url": "https://example.com/products",
                    "userAgent": "test-agent/1.0",
                    "connectionString": null,
                    "instance": null,
                    "name": null,
                    "operation": null,
                    "statement": null,
                    "system": null,
                    "user": null,
                    "document": null,
                    "graphQLOperation": null,
                    "selection": null
                  }
                },
                {
                  "spanId": "database-span",
                  "parentSpanId": "",
                  "spanName": "database-span",
                  "spanKind": "SERVER",
                  "durationMs": 1,
                  "start": 0,
                  "statusCode": "OK",
                  "statusMessage": "",
                  "resourceAttributes": [],
                  "spanAttributes": [],
                  "events": [],
                  "data": {
                    "kind": "database",
                    "flavor": null,
                    "method": null,
                    "scheme": null,
                    "statusCode": null,
                    "url": "postgresql://localhost/orders",
                    "userAgent": null,
                    "connectionString": "Host=localhost",
                    "instance": "primary",
                    "name": "orders",
                    "operation": "SELECT",
                    "statement": "SELECT * FROM orders",
                    "system": "postgresql",
                    "user": "app",
                    "document": null,
                    "graphQLOperation": null,
                    "selection": null
                  }
                },
                {
                  "spanId": "operation-span",
                  "parentSpanId": "",
                  "spanName": "operation-span",
                  "spanKind": "SERVER",
                  "durationMs": 1,
                  "start": 0,
                  "statusCode": "OK",
                  "statusMessage": "",
                  "resourceAttributes": [],
                  "spanAttributes": [],
                  "events": [],
                  "data": {
                    "kind": "graphql.operation",
                    "flavor": null,
                    "method": null,
                    "scheme": null,
                    "statusCode": null,
                    "url": null,
                    "userAgent": null,
                    "connectionString": null,
                    "instance": null,
                    "name": null,
                    "operation": null,
                    "statement": null,
                    "system": null,
                    "user": null,
                    "document": {
                      "body": "query GetOrders { orders { id } }",
                      "id": "document-id"
                    },
                    "graphQLOperation": {
                      "hash": "operation-hash",
                      "kind": "query",
                      "name": "GetOrders"
                    },
                    "selection": null
                  }
                },
                {
                  "spanId": "resolver-span",
                  "parentSpanId": "",
                  "spanName": "resolver-span",
                  "spanKind": "SERVER",
                  "durationMs": 1,
                  "start": 0,
                  "statusCode": "OK",
                  "statusMessage": "",
                  "resourceAttributes": [],
                  "spanAttributes": [],
                  "events": [],
                  "data": {
                    "kind": "graphql.resolver",
                    "flavor": null,
                    "method": null,
                    "scheme": null,
                    "statusCode": null,
                    "url": null,
                    "userAgent": null,
                    "connectionString": null,
                    "instance": null,
                    "name": null,
                    "operation": null,
                    "statement": null,
                    "system": null,
                    "user": null,
                    "document": null,
                    "graphQLOperation": null,
                    "selection": {
                      "field": {
                        "coordinate": "Query.orders",
                        "declaringType": "Query",
                        "name": "orders"
                      },
                      "name": "orders",
                      "path": "orders",
                      "type": "[Order!]!"
                    }
                  }
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
        IReadOnlyList<TelemetryAttribute>? resourceAttributes = null,
        TraceSpanData? data = null)
        => new(id, parent, id, "SERVER", duration, 0, status, string.Empty, resourceAttributes ?? [], [], [], data);
}
