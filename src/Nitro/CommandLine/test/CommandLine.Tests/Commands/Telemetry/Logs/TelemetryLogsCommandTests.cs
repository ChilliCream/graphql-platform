using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Logs;

public sealed class TelemetryLogsCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task ListHelp_Should_ReturnSuccess()
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
              --severity <Debug|Error|Fatal|Info|Trace|Warn>  Only include logs at or above this severity
              --trace-id <trace-id>                           Only include logs from a trace
              --search <search>                               Search log messages
              --cloud-url <cloud-url>                         The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                             The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                  Show help and usage information

            Example:
              nitro telemetry logs list
              nitro telemetry logs list --service checkout --severity error --since 2h
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
            hint: run `nitro login`.
            """);
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
            hint: examples: `severity:error`, `@resource.service.name:checkout`, or `exception.type:TimeoutException`
            """);
    }

    [Fact]
    public async Task List_Should_ForwardShortcutOptionsToFilter_When_Specified()
    {
        // arrange
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
        SetupListLogs(CreateLogRow());

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [
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
              "returned": 1,
              "total": null,
              "hasMore": false
            }
            """);
    }

    [Fact]
    public async Task List_Should_WriteSuggestionHint_When_FilteredResultHasAnUnknownKey()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListLogs();
        SetupListAttributeKeys(
            keys:
            [
                new AttributeKeyRow("Log", "http.response.status_code"),
                new AttributeKeyRow("Log", "http.status_code")
            ]);

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "list", "--filter", "http.statuscode:>=500");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [],
              "returned": 0,
              "total": null,
              "hasMore": false,
              "hint": "no results; unknown key \u0027http.statuscode\u0027, did you mean http.status_code, http.response.status_code? Run nitro telemetry attributes keys --signal logs to list keys."
            }
            """);
        TelemetryClientMock.Verify(
            x =>
                x.ListAttributeKeysAsync(
                    WorkspaceId,
                    OpenTelemetrySignalKind.Logs,
                    null,
                    null,
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(),
                    50,
                    null,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_WriteLogDetail_When_LogExists(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupGetLog(CreateLog());

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "show", "log-1");

        // assert
        result.AssertSuccess(
            """
            {
              "id": "log-1",
              "epoch": 1767225600123,
              "severityText": "ERROR",
              "severityNumber": 17,
              "serviceName": "products",
              "body": "Request failed",
              "traceId": "trace-1",
              "spanId": "span-1",
              "bodyDetail": {
                "json": null,
                "kind": "String",
                "message": "Request failed"
              },
              "attributes": [
                {
                  "key": "code.function",
                  "value": "CheckoutHandler.Handle"
                },
                {
                  "key": "code.filepath",
                  "value": "src/CheckoutHandler.cs"
                },
                {
                  "key": "code.lineno",
                  "value": "42"
                },
                {
                  "key": "exception.type",
                  "value": "System.TimeoutException"
                },
                {
                  "key": "exception.message",
                  "value": "The operation timed out."
                },
                {
                  "key": "exception.stacktrace",
                  "value": "at CheckoutHandler.Handle()\n   at Program.Main()"
                }
              ],
              "resourceAttributes": [
                {
                  "key": "service.name",
                  "value": "products"
                },
                {
                  "key": "deployment.environment",
                  "value": "production"
                }
              ],
              "scope": {
                "name": "OpenTelemetry.Instrumentation",
                "schemaUrl": "https://opentelemetry.io/schemas/1.0.0",
                "version": "1.0.0",
                "attributes": [
                  {
                    "key": "scope.attribute",
                    "value": "scope-value"
                  }
                ]
              },
              "codeFunction": "CheckoutHandler.Handle",
              "codeFilePath": "src/CheckoutHandler.cs",
              "codeLineNumber": 42
            }
            """);
    }

    private void SetupListLogs(params LogRow[] logs)
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
            .ReturnsAsync(new ConnectionPage<LogRow>(logs, null, false));
    }

    private void SetupGetLog(Log log)
    {
        TelemetryClientMock
            .Setup(x => x.GetLogAsync(WorkspaceId, "log-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);
    }

    private static LogRow CreateLogRow()
        => new("log-1", 1767225600123, "ERROR", 17, "Request failed", "trace-1", "span-1", "products");

    private static Log CreateLog()
        => new(
            "log-1",
            1767225600123,
            "ERROR",
            17,
            "Request failed",
            "trace-1",
            "span-1",
            new LogBodyDetail(null, "String", "Request failed"),
            [
                new TypedTelemetryAttribute("code.function", null, null, null, "CheckoutHandler.Handle"),
                new TypedTelemetryAttribute("code.filepath", null, null, null, "src/CheckoutHandler.cs"),
                new TypedTelemetryAttribute("code.lineno", null, null, 42, null),
                new TypedTelemetryAttribute("exception.type", null, null, null, "System.TimeoutException"),
                new TypedTelemetryAttribute("exception.message", null, null, null, "The operation timed out."),
                new TypedTelemetryAttribute(
                    "exception.stacktrace",
                    null,
                    null,
                    null,
                    "at CheckoutHandler.Handle()\n   at Program.Main()")
            ],
            [
                new TelemetryAttribute("service.name", "products"),
                new TelemetryAttribute("deployment.environment", "production")
            ],
            new TelemetryScope(
                "OpenTelemetry.Instrumentation",
                "https://opentelemetry.io/schemas/1.0.0",
                "1.0.0",
                [new TypedTelemetryAttribute("scope.attribute", null, null, null, "scope-value")]));
}
