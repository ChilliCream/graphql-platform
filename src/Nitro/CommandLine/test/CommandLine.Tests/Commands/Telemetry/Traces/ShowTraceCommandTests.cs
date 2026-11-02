using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Traces;

public sealed class ShowTraceCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    private const string TraceId = "trace-id";

    [Fact]
    public async Task Show_Should_ReturnSuccess_When_HelpIsRequested()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show a telemetry trace.

            Usage:
              nitro telemetry traces show <trace-id> [options]

            Arguments:
              <trace-id>  The trace ID

            Options:
              --span <span>                  Focus on the subtree rooted at a span ID
              --since <since>                The earliest timestamp to include [default: 12/31/2025 23:30:00 +00:00]
              --until <until>                The latest timestamp to include [default: 01/01/2026 00:00:00 +00:00]
              --workspace-id <workspace-id>  The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>            The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                 Show help and usage information

            Example:
              nitro telemetry traces show "<trace-id>"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_ReturnError_When_NoSessionAndNoWorkspaceId(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_ReturnError_When_WorkspaceIsUnavailable(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSession();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            """);
    }

    [Fact]
    public async Task Show_Should_ReturnError_When_TraceIdIsMissing()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show");

        // assert
        result.StdErr.MatchInlineSnapshot("Required argument missing for command: 'show'.");
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_PassHiddenSeekerAndSpan_When_OptionsAreSpecified(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
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

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_NotDeriveSeekerFromTimeBounds_When_SinceAndUntilAreSpecified(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
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

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task Show_Should_RenderSummaryTopOperationsAndTree_When_TraceExists(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
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
    public async Task Show_Should_WriteTraceJson_When_TraceExistsAndOutputIsJson()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupSessionWithWorkspace();
        SetupGetTrace(CreateTrace());

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertSuccess(
            """
            {
              "traceId": "trace-id",
              "spanCount": 2,
              "spansTruncated": false,
              "totalDurationMs": 25,
              "spans": [
                {
                  "spanId": "root",
                  "parentSpanId": "",
                  "spanName": "root",
                  "spanKind": "SERVER",
                  "durationMs": 20,
                  "start": 0,
                  "statusCode": "OK",
                  "statusMessage": "",
                  "resourceAttributes": [
                    {
                      "key": "service.name",
                      "value": "orders"
                    }
                  ],
                  "spanAttributes": [],
                  "events": [],
                  "data": null
                },
                {
                  "spanId": "child",
                  "parentSpanId": "root",
                  "spanName": "child",
                  "spanKind": "SERVER",
                  "durationMs": 5,
                  "start": 0,
                  "statusCode": "ERROR",
                  "statusMessage": "",
                  "resourceAttributes": [
                    {
                      "key": "service.name",
                      "value": "orders"
                    }
                  ],
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
                            new GraphQLTraceDocument("query GetOrders { orders { id } }", "document-id"),
                            new GraphQLTraceOperation("operation-hash", "query", "GetOrders"))),
                    CreateSpan(
                        "resolver-span",
                        data: new GraphQLResolverTraceSpanData(
                            new GraphQLTraceSelection(
                                new GraphQLTraceField("Query.orders", "Query", "orders"),
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
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_ReturnError_When_TraceIsNotFound(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupGetTrace(null);

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertError(
            """
            The trace 'trace-id' was not found.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_ReturnError_When_TraceHasNoSpans(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupGetTrace(new Trace(0, false, 0, []));

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertError(
            """
            The trace 'trace-id' was not found.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task Show_Should_LabelSummaryAsServerCapped_When_TraceIsTruncatedAndCountIsUnknown(
        InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
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

    [Fact]
    public async Task Show_Should_WriteTruncationInJson_When_TraceIsTruncatedAndOutputIsJson()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupSessionWithWorkspace();
        SetupGetTrace(new Trace(null, true, 0, [CreateSpan("root", duration: 20)]));

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertSuccess(
            """
            {
              "traceId": "trace-id",
              "spanCount": null,
              "spansTruncated": true,
              "totalDurationMs": 0,
              "spans": [
                {
                  "spanId": "root",
                  "parentSpanId": "",
                  "spanName": "root",
                  "spanKind": "SERVER",
                  "durationMs": 20,
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
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_ReturnError_When_GetTraceThrows(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupGetTraceException();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "show", TraceId);

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
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

    private void SetupGetTraceException()
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
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
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
