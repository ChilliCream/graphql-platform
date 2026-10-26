using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Logs;

public sealed class ShowLogCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_Should_ReturnSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "show", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show a telemetry log.

            Usage:
              nitro telemetry logs show <log-id> [options]

            Arguments:
              <log-id>  The log ID

            Options:
              --workspace-id <workspace-id>  The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>            The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                 Show help and usage information

            Example:
              nitro telemetry logs show "<log-id>"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_ReturnError_When_AuthenticationIsUnavailable(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "show", "log-1");

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
        var result = await ExecuteCommandAsync("telemetry", "logs", "show", "log-1");

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            """);
    }

    [Fact]
    public async Task Show_Should_ReturnError_When_LogIdIsMissing()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "show");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Required argument missing for command: 'show'.
            """);
        Assert.Equal(1, result.ExitCode);
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
                  "key": "http.request.duration",
                  "value": "1234.5"
                },
                {
                  "key": "retry.enabled",
                  "value": "true"
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

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_ReturnError_When_LogDoesNotExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        TelemetryClientMock
            .Setup(x => x.GetLogAsync(WorkspaceId, "log-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Log?)null);

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "show", "log-1");

        // assert
        result.AssertError(
            """
            The log 'log-1' was not found.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_ReturnError_When_ClientThrows(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        TelemetryClientMock
            .Setup(x => x.GetLogAsync(WorkspaceId, "log-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));

        // act
        var result = await ExecuteCommandAsync("telemetry", "logs", "show", "log-1");

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }

    private void SetupGetLog(Log log)
    {
        TelemetryClientMock
            .Setup(x => x.GetLogAsync(WorkspaceId, "log-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);
    }

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
                new TypedTelemetryAttribute("http.request.duration", null, 1234.5, null, null),
                new TypedTelemetryAttribute("retry.enabled", true, null, null, null),
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
