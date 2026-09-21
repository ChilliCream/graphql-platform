using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Traces;

public sealed class ListTraceCommandTests(NitroCommandFixture fixture)
    : TelemetryCommandTestBase(fixture)
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task List_Should_ReturnSuccess_When_HelpIsRequested()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List telemetry traces in the current workspace.

            Usage:
              nitro telemetry traces list [options]

            Options:
              --service <service>            Limit results to a service [env: NITRO_SERVICE]
              --env <env>                    Limit results to an environment; can be used multiple times [env: NITRO_ENV]
              --filter <filter>              Filter results using the telemetry filter grammar
              --has-error                    Only include results with errors
              --min-duration <min-duration>  Only include spans lasting at least this many milliseconds
              --span-kind <span-kind>        Limit results to a span kind; can be used multiple times
              --search <search>              Search span names or log messages
              --since <since>                The earliest timestamp to include [env: NITRO_SINCE] [default: 12/31/2025 23:30:00 +00:00]
              --until <until>                The latest timestamp to include [env: NITRO_UNTIL] [default: 01/01/2026 00:00:00 +00:00]
              --limit <limit>                The maximum number of results to show [env: NITRO_LIMIT]
              --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>            The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json|ndjson>         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                 Show help and usage information

            Example:
              nitro telemetry traces list
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReturnError_When_AuthenticationIsUnavailable(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            hint: run `nitro login`.
            """);
    }

    [Fact]
    public async Task List_Should_RenderFilterDiagnostic_When_FilterIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list",
            "--filter",
            "service.name:");

        // assert
        result.AssertError(
            """
            filter: Missing value in key:value pair at column 13
            service.name:
                        ^
            hint: examples: `status:error`, `duration:>=100`, or `@resource.service.name:checkout`
            """);
        TelemetryClientMock.Verify(
            x => x.ListTracesAsync(
                It.IsAny<string>(),
                It.IsAny<OpenTelemetryFilterInput?>(),
                It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<IReadOnlyList<OpenTelemetrySpanKind>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task List_Should_DefaultToEntrySpanKinds_When_NoneAreSpecified()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListTraces(CreateTrace());

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list");

        // assert
        result.AssertSuccess();
        Assert.Equal(
            [OpenTelemetrySpanKind.Server, OpenTelemetrySpanKind.Consumer],
            (IReadOnlyList<OpenTelemetrySpanKind>)TelemetryClientMock.Invocations.Single().Arguments[3]!);
    }

    [Fact]
    public async Task List_Should_CompileErrorAndDurationFlags_When_Provided()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListTraces();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list",
            "--filter",
            "http.status_code:>=500",
            "--has-error",
            "--min-duration",
            "100",
            "--search",
            "timeout",
            "--service",
            "checkout");
        var filter = (OpenTelemetryFilterInput?)TelemetryClientMock.Invocations
            .Single(invocation => invocation.Method.Name == nameof(ITelemetryClient.ListTracesAsync))
            .Arguments[1];

        // assert
        result.AssertSuccess();
        JsonSerializer.Serialize(filter, s_jsonSerializerOptions).MatchInlineSnapshot(
            """
            {"and":[{"attribute":{"condition":{"gte":{"int":500}},"key":"http.status_code"}},{"attribute":{"condition":{"eq":{"string":"error"}},"key":"status"}},{"attribute":{"condition":{"gte":{"int":100}},"key":"duration"}},{"attribute":{"condition":{"matches":"*timeout*"},"key":"span.name"}},{"attribute":{"condition":{"eq":{"string":"checkout"}},"key":"service.name","kind":"Resource"}}]}
            """);
    }

    [Fact]
    public async Task List_Should_ReplaceDefaultSpanKinds_When_SpanKindsAreSpecified()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListTraces(CreateTrace());

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list",
            "--span-kind",
            "client",
            "--span-kind",
            "producer");

        // assert
        result.AssertSuccess();
        Assert.Equal(
            [OpenTelemetrySpanKind.Client, OpenTelemetrySpanKind.Producer],
            (IReadOnlyList<OpenTelemetrySpanKind>)TelemetryClientMock.Invocations.Single().Arguments[3]!);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReturnSuccess_When_A_TraceExists(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListTraces(CreateTrace());

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list");

        // assert
        result.AssertSuccess(
            mode switch
            {
                InteractionMode.Interactive =>
                    """
                    ┌──────────────┬──────────┬───────────────┬───────────────┬────────┬──────────┐
                    │ Start        │ Service  │ Name          │ Duration (ms) │ Status │ Trace ID │
                    ├──────────────┼──────────┼───────────────┼───────────────┼────────┼──────────┤
                    │ 23:30:00.123 │ products │ GET /products │ 125.5         │ Error  │ trace-1  │
                    └──────────────┴──────────┴───────────────┴───────────────┴────────┴──────────┘
                    """,
                _ =>
                    """
                    {"items":[
                    {"start":"2025-12-31T23:30:00.123+00:00","service":"products","name":"GET /products","durationMs":125.5,"status":"Error","traceId":"trace-1","spanId":"span-1","seeker":"seeker-1"}
                    ],"returned":1,"total":null,"hasMore":false}
                    """
            });
    }

    [Fact]
    public async Task List_Should_RenderTraceFieldsAndHint_When_JsonOutputIsRequested()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupSessionWithWorkspace();
        SetupListTracesWithMore(
            CreateTrace(start: 1767223800123),
            CreateTrace(traceId: "trace-2", start: 1767223801123));

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list");

        // assert
        result.AssertSuccess(
            """
            {"items":[
            {"start":"2025-12-31T23:30:01.123+00:00","service":"products","name":"GET /products","durationMs":125.5,"status":"Error","traceId":"trace-2","spanId":"span-1","seeker":"seeker-1"},
            {"start":"2025-12-31T23:30:00.123+00:00","service":"products","name":"GET /products","durationMs":125.5,"status":"Error","traceId":"trace-1","spanId":"span-1","seeker":"seeker-1"}
            ],"returned":2,"total":null,"hasMore":true,"hint":"showing 2 (more), narrow with --since, --service or --filter, or raise --limit"}
            """);
    }

    [Fact]
    public async Task List_Should_WriteSuggestionHintInAgentEnvelope_When_FilteredResultHasAnUnknownKey()
    {
        // arrange
        SetupAgentMode();
        SetupSessionWithWorkspace();
        SetupListTraces();
        SetupListAttributeKeys(
            keys:
            [
                new AttributeKeyRow("Span", "http.response.status_code"),
                new AttributeKeyRow("Span", "http.status_code")
            ]);

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list",
            "--filter",
            "http.statuscode:>=500");

        // assert
        result.AssertSuccess(
            """
            {"items":[],"returned":0,"total":null,"hasMore":false,"hint":"no results; unknown key \u0027http.statuscode\u0027, did you mean http.status_code, http.response.status_code? Run nitro telemetry attributes keys --signal traces to list keys."}
            """);
        TelemetryClientMock.Verify(
            x => x.ListAttributeKeysAsync(
                WorkspaceId,
                OpenTelemetrySignalKind.Traces,
                null,
                null,
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                50,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task List_Should_NotWriteSuggestionHint_When_FilterKeyIsKnown()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListTraces();
        SetupListAttributeKeys(keys: [new AttributeKeyRow("Span", "http.statuscode")]);

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list",
            "--filter",
            "http.statuscode:>=500");

        // assert
        result.AssertSuccess(
            """
            {"items":[],"returned":0,"total":null,"hasMore":false}
            """);
    }

    private void SetupListTraces(
        params TraceRow[] traces)
        => SetupListTraces(traces, hasNextPage: false);

    private void SetupListTracesWithMore(params TraceRow[] traces)
        => SetupListTraces(traces, hasNextPage: true);

    private void SetupListTraces(
        TraceRow[] traces,
        bool hasNextPage)
    {
        TelemetryClientMock.Setup(x => x.ListTracesAsync(
                WorkspaceId,
                It.IsAny<OpenTelemetryFilterInput?>(),
                It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<IReadOnlyList<OpenTelemetrySpanKind>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                20,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<TraceRow>(traces, null, hasNextPage));
    }

    private static TraceRow CreateTrace(
        string traceId = "trace-1",
        double start = 1767223800123)
        => new(
            traceId,
            "span-1",
            "seeker-1",
            "GET /products",
            "SERVER",
            125.5,
            start,
            "Error",
            "products");
}
