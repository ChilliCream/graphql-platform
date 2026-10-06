using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Traces;

public sealed class ListTraceCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
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
        var result = await ExecuteCommandAsync("telemetry", "traces", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List telemetry traces in the current workspace.

            Usage:
              nitro telemetry traces list [options]

            Options:
              --service <service>            Limit results to a service
              --env <env>                    Limit results to an environment; can be used multiple times
              --filter <filter>              Filter results using the telemetry filter grammar
              --has-error                    Only include results with errors
              --min-duration <min-duration>  Only include spans lasting at least this many milliseconds
              --span-kind <span-kind>        Limit results to a span kind; can be used multiple times
              --search <search>              Search span names
              --since <since>                The earliest timestamp to include [default: 12/31/2025 23:30:00 +00:00]
              --until <until>                The latest timestamp to include [default: 01/01/2026 00:00:00 +00:00]
              --limit <limit>                The maximum number of results to show
              --cursor <cursor>              The pagination cursor to resume from [env: NITRO_CURSOR]
              --workspace-id <workspace-id>  The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>            The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                 Show help and usage information

            Example:
              nitro telemetry traces list
              nitro telemetry traces list --filter "http.response.status_code:>=500"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReturnError_When_NoSessionAndNoWorkspaceId(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list");

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
    public async Task List_Should_ReturnError_When_WorkspaceIsUnavailable(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSession();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list");

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            """);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_SinceIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list", "--since", "yesterday");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--since' received an invalid value: yesterday
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_UntilIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list", "--until", "tomorrow");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--until' received an invalid value: tomorrow
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_SinceIsOlderThanSixtyDays()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list", "--since", "61d");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--since' cannot be more than 60 days in the past.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_SinceIsNotBeforeUntil()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list", "--since", "1h", "--until", "2h");

        // assert
        result.StdErr.MatchInlineSnapshot("Option '--since' must be earlier than '--until'.");
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData("--limit", "0", "Option '--limit' must be a positive number.")]
    [InlineData("--limit", "-1", "Option '--limit' must be a positive number.")]
    [InlineData("--min-duration", "-1", "Option '--min-duration' must not be negative.")]
    public async Task List_Should_ReturnError_When_NumericOptionIsOutOfRange(string option, string value, string error)
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list", option, value);

        // assert
        result.StdErr.MatchInlineSnapshot(error);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_SpanKindIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list", "--span-kind", "unknown");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Cannot parse argument 'unknown' for option '--span-kind' as expected type 'ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options.TelemetrySpanKind'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_RenderFilterDiagnostic_When_FilterIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list", "--filter", "service.name:");

        // assert
        result.AssertError(
            """
            filter: Missing value in key:value pair at column 13
            service.name:
                        ^
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_DefaultToEntrySpanKinds_When_NoneAreSpecified(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListTraces();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list");

        // assert
        result.AssertSuccess();
        VerifyListedSpanKinds(OpenTelemetrySpanKind.Server, OpenTelemetrySpanKind.Consumer);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReplaceDefaultSpanKinds_When_SpanKindsAreSpecified(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListTraces();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "traces",
            "list",
            "--span-kind",
            "client",
            "--span-kind",
            "producer",
            "--span-kind",
            "server",
            "--span-kind",
            "consumer",
            "--span-kind",
            "internal");

        // assert
        result.AssertSuccess();
        VerifyListedSpanKinds(
            OpenTelemetrySpanKind.Client,
            OpenTelemetrySpanKind.Producer,
            OpenTelemetrySpanKind.Server,
            OpenTelemetrySpanKind.Consumer,
            OpenTelemetrySpanKind.Internal);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ForwardFilterAndShortcutOptions_When_Provided(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
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
        var filter = (OpenTelemetryFilterInput?)
            TelemetryClientMock
                .Invocations.Single(invocation => invocation.Method.Name == nameof(ITelemetryClient.ListTracesAsync))
                .Arguments[1];

        // assert
        result.AssertSuccess();
        JsonSerializer
            .Serialize(filter, s_jsonSerializerOptions)
            .MatchInlineSnapshot(
                """
                {"and":[{"attribute":{"condition":{"gte":{"int":500}},"key":"http.status_code"}},{"attribute":{"condition":{"eq":{"string":"error"}},"key":"status"}},{"attribute":{"condition":{"gte":{"int":100}},"key":"duration"}},{"attribute":{"condition":{"matches":"*timeout*"},"key":"span.name"}},{"attribute":{"condition":{"eq":{"string":"checkout"}},"key":"service.name","kind":"Resource"}}]}
                """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReturnSuccess_When_TraceExists(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListTraces(CreateTrace());

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "start": "2025-12-31T23:30:00.123+00:00",
                  "service": "products",
                  "name": "GET /products",
                  "durationMs": 125.5,
                  "status": "Error",
                  "traceId": "trace-1",
                  "spanId": "span-1",
                  "seeker": "seeker-1"
                }
              ],
              "cursor": null
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReturnSuccess_When_NoTraceExists(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListTraces();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReturnEmptyPage_When_FilterHasNoMatches(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListTraces();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list", "--filter", "http.statuscode:>=500");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_OrderNewestFirstAndReturnCursor_When_ResultHasMoreItems(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListTracesWithMore(
            CreateTrace(start: 1767223800123),
            CreateTrace(traceId: "trace-2", start: 1767223801123));

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "start": "2025-12-31T23:30:01.123+00:00",
                  "service": "products",
                  "name": "GET /products",
                  "durationMs": 125.5,
                  "status": "Error",
                  "traceId": "trace-2",
                  "spanId": "span-1",
                  "seeker": "seeker-1"
                },
                {
                  "start": "2025-12-31T23:30:00.123+00:00",
                  "service": "products",
                  "name": "GET /products",
                  "durationMs": 125.5,
                  "status": "Error",
                  "traceId": "trace-1",
                  "spanId": "span-1",
                  "seeker": "seeker-1"
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReturnError_When_ListTracesThrows(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListTracesException();

        // act
        var result = await ExecuteCommandAsync("telemetry", "traces", "list");

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive, false)]
    [InlineData(InteractionMode.Interactive, true)]
    [InlineData(InteractionMode.NonInteractive, false)]
    [InlineData(InteractionMode.NonInteractive, true)]
    [InlineData(InteractionMode.JsonOutput, false)]
    [InlineData(InteractionMode.JsonOutput, true)]
    public async Task List_Should_ResumeFromCursor_When_CursorIsSpecified(
        InteractionMode mode,
        bool useEnvironmentCursor)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupEnvironmentVariable("CURSOR", "cursor-from-env");
        var cursor = useEnvironmentCursor ? "cursor-from-env" : "cursor-from-option";
        string[] cursorArguments = useEnvironmentCursor ? [] : ["--cursor", "cursor-from-option"];
        TelemetryClientMock
            .Setup(x =>
                x.ListTracesAsync(
                    WorkspaceId,
                    It.IsAny<OpenTelemetryFilterInput?>(),
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<IReadOnlyList<OpenTelemetrySpanKind>?>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(),
                    20,
                    cursor,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<TraceRow>(
                [CreateTrace(start: 1767223800123), CreateTrace(traceId: "trace-2", start: 1767223801123)],
                "cursor-2",
                true));

        // act
        var result = await ExecuteCommandAsync(["telemetry", "traces", "list", .. cursorArguments]);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "start": "2025-12-31T23:30:01.123+00:00",
                  "service": "products",
                  "name": "GET /products",
                  "durationMs": 125.5,
                  "status": "Error",
                  "traceId": "trace-2",
                  "spanId": "span-1",
                  "seeker": "seeker-1"
                },
                {
                  "start": "2025-12-31T23:30:00.123+00:00",
                  "service": "products",
                  "name": "GET /products",
                  "durationMs": 125.5,
                  "status": "Error",
                  "traceId": "trace-1",
                  "spanId": "span-1",
                  "seeker": "seeker-1"
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    private void SetupListTraces(params TraceRow[] traces) => SetupListTraces(traces, hasNextPage: false);

    private void SetupListTracesWithMore(params TraceRow[] traces) => SetupListTraces(traces, hasNextPage: true);

    private void SetupListTraces(TraceRow[] traces, bool hasNextPage)
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListTracesAsync(
                    WorkspaceId,
                    It.IsAny<OpenTelemetryFilterInput?>(),
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<IReadOnlyList<OpenTelemetrySpanKind>?>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(),
                    20,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<TraceRow>(traces, hasNextPage ? "cursor-2" : null, hasNextPage));
    }

    private void SetupListTracesException()
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListTracesAsync(
                    WorkspaceId,
                    It.IsAny<OpenTelemetryFilterInput?>(),
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<IReadOnlyList<OpenTelemetrySpanKind>?>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(),
                    20,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    private void VerifyListedSpanKinds(params OpenTelemetrySpanKind[] expected)
    {
        TelemetryClientMock.Verify(
            x =>
                x.ListTracesAsync(
                    WorkspaceId,
                    It.IsAny<OpenTelemetryFilterInput?>(),
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.Is<IReadOnlyList<OpenTelemetrySpanKind>?>(spanKinds =>
                        spanKinds != null && spanKinds.SequenceEqual(expected)
                    ),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(),
                    20,
                    null,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static TraceRow CreateTrace(string traceId = "trace-1", double start = 1767223800123)
        => new(traceId, "span-1", "seeker-1", "GET /products", "SERVER", 125.5, start, "Error", "products");
}
