using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Logs;

public sealed class ListLogsCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Help_Should_ReturnSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List telemetry logs in the current workspace.

            Usage:
              nitro telemetry logs list [options]

            Options:
              --service <service>                             Limit results to a service
              --env <env>                                     Limit results to an environment; can be used multiple times
              --filter <filter>                               Filter results using the telemetry filter grammar
              --since <since>                                 The earliest timestamp to include [default: 12/31/2025 23:30:00 +00:00]
              --until <until>                                 The latest timestamp to include [default: 01/01/2026 00:00:00 +00:00]
              --limit <limit>                                 The maximum number of results to show
              --cursor <cursor>                               The pagination cursor to resume from [env: NITRO_CURSOR]
              --severity <Debug|Error|Fatal|Info|Trace|Warn>  Only include logs at or above this severity
              --trace-id <trace-id>                           Only include logs from a trace
              --search <search>                               Search log messages
              --workspace-id <workspace-id>                   The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>                         The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                             The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                  Show help and usage information

            Example:
              nitro telemetry logs list
              nitro telemetry logs list --service checkout --severity error --since 2h
              nitro telemetry logs list --filter "severity:error"
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
        var result = await ExecuteCommandAsync("telemetry", "logs", "list");

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
        var result = await ExecuteCommandAsync("telemetry", "logs", "list");

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
        var result = await ExecuteCommandAsync("telemetry", "logs", "list", "--since", "yesterday");

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
        var result = await ExecuteCommandAsync("telemetry", "logs", "list", "--until", "yesterday");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--until' received an invalid value: yesterday
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_SinceIsOlderThanSixtyDays()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list", "--since", "61d");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--since' cannot be more than 60 days in the past.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_SinceIsNotEarlierThanUntil()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list", "--since", "1h", "--until", "2h");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--since' must be earlier than '--until'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_LimitIsZero()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list", "--limit", "0");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--limit' must be a positive number.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_SeverityIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list", "--severity", "verbose");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Cannot parse argument 'verbose' for option '--severity' as expected type 'ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options.TelemetrySeverity'. Did you mean one of the following?
            Debug
            Error
            Fatal
            Info
            Trace
            Warn
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_RenderFilterDiagnostic_When_FilterIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list", "--filter", "service.name:");

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
    public async Task List_Should_ForwardShortcutOptionsToFilter_When_Specified(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListLogs();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "logs",
            "list",
            "--severity",
            "warn",
            "--trace-id",
            "trace-1",
            "--search",
            "timeout",
            "--service",
            "checkout");
        var filter = (OpenTelemetryFilterInput?)
            TelemetryClientMock
                .Invocations.Single(invocation => invocation.Method.Name == nameof(ITelemetryClient.ListLogsAsync))
                .Arguments[1];

        // assert
        result.AssertSuccess();
        JsonSerializer
            .Serialize(filter, s_jsonSerializerOptions)
            .MatchInlineSnapshot(
                """
                {"and":[{"attribute":{"condition":{"in":[{"string":"warn"},{"string":"error"},{"string":"fatal"}]},"key":"severity"}},{"attribute":{"condition":{"eq":{"string":"trace-1"}},"key":"trace.id"}},{"attribute":{"condition":{"matches":"*timeout*"},"key":"log.message"}},{"attribute":{"condition":{"eq":{"string":"checkout"}},"key":"service.name","kind":"Resource"}}]}
                """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReturnSuccess_When_LogsExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListLogs(logs: [CreateLogRow("log-1", 1767225600123), CreateLogRow("log-2", 1767225660456)]);

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "log-2",
                  "epoch": 1767225660456,
                  "severityText": "ERROR",
                  "severityNumber": 17,
                  "serviceName": "products",
                  "body": "Request failed",
                  "traceId": "trace-1",
                  "spanId": "span-1"
                },
                {
                  "id": "log-1",
                  "epoch": 1767225600123,
                  "severityText": "ERROR",
                  "severityNumber": 17,
                  "serviceName": "products",
                  "body": "Request failed",
                  "traceId": "trace-1",
                  "spanId": "span-1"
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
    public async Task List_Should_ReturnSuccess_When_NoLogsExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListLogs();

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list");

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
    public async Task List_Should_ReturnCursor_When_MoreLogsExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListLogs(hasNextPage: true, logs: [CreateLogRow("log-1", 1767225600123)]);

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "log-1",
                  "epoch": 1767225600123,
                  "severityText": "ERROR",
                  "severityNumber": 17,
                  "serviceName": "products",
                  "body": "Request failed",
                  "traceId": "trace-1",
                  "spanId": "span-1"
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
    public async Task List_Should_ReturnEmptyPage_When_FilterHasNoMatches(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListLogs();

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list", "--filter", "http.statuscode:>=500");

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
    public async Task List_Should_ReturnError_When_ClientThrows(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListLogsException();

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list");

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
                x.ListLogsAsync(
                    WorkspaceId,
                    It.IsAny<OpenTelemetryFilterInput?>(),
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(),
                    50,
                    cursor,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<LogRow>([CreateLogRow("log-1", 1767225600123)], "cursor-2", true));

        // act
        var result = await ExecuteCommandAsync(["telemetry", "logs", "list", .. cursorArguments]);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "log-1",
                  "epoch": 1767225600123,
                  "severityText": "ERROR",
                  "severityNumber": 17,
                  "serviceName": "products",
                  "body": "Request failed",
                  "traceId": "trace-1",
                  "spanId": "span-1"
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    private void SetupListLogs(bool hasNextPage = false, params LogRow[] logs)
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListLogsAsync(
                    WorkspaceId,
                    It.IsAny<OpenTelemetryFilterInput?>(),
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(),
                    50,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<LogRow>(logs, hasNextPage ? "cursor-2" : null, hasNextPage));
    }

    private void SetupListLogsException()
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListLogsAsync(
                    WorkspaceId,
                    It.IsAny<OpenTelemetryFilterInput?>(),
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(),
                    50,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    private static LogRow CreateLogRow(string id, double start)
        => new(id, start, "ERROR", 17, "Request failed", "trace-1", "span-1", "products");
}
