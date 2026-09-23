using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Logs;

public sealed class TelemetryLogsCommandTests(NitroCommandFixture fixture)
    : TelemetryCommandTestBase(fixture)
{
    [Fact]
    public async Task ListHelp_Should_ReturnSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "logs",
            "list",
            "--help");

        // assert
        result.AssertSuccess();
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
            "logs",
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
            "logs",
            "list",
            "--filter",
            "service.name:");

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
    public async Task List_Should_CompileEveryHigherSeverity_When_SeverityIsSpecified()
    {
        // arrange
        SetupSessionWithWorkspace();
        TelemetryClientMock.Setup(x => x.ListLogsAsync(
                WorkspaceId,
                It.Is<OpenTelemetryFilterInput?>(filter => IsWarnOrHigherFilter(filter)),
                It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                50,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<LogRow>([], null, false));

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "logs",
            "list",
            "--severity",
            "warn");

        // assert
        result.AssertSuccess(
            """
            {"items":[],"returned":0,"total":null,"hasMore":false}
            """);
    }

    [Fact]
    public async Task List_Should_CompileTraceIdFilter_When_TraceIdIsSpecified()
    {
        // arrange
        SetupSessionWithWorkspace();
        TelemetryClientMock.Setup(x => x.ListLogsAsync(
                WorkspaceId,
                It.Is<OpenTelemetryFilterInput?>(filter => IsTraceIdFilter(filter)),
                It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                50,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<LogRow>([], null, false));

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "logs",
            "list",
            "--trace-id",
            "trace-1");

        // assert
        result.AssertSuccess(
            """
            {"items":[],"returned":0,"total":null,"hasMore":false}
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
        SetupListLogs(logs: [CreateLogRow()]);

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "logs",
            "list");

        // assert
        result.AssertSuccess(
            """
            {"items":[
            {"id":"log-1","epoch":1767225600123,"severityText":"ERROR","severityNumber":17,"serviceName":"products","body":"Request failed","traceId":"trace-1","spanId":"span-1"}
            ],"returned":1,"total":null,"hasMore":false}
            """);
    }

    [Fact]
    public async Task List_Should_WriteEmptyResult_When_NoLogsExist()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSessionWithWorkspace();
        SetupListLogs();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "logs",
            "list");

        // assert
        result.AssertSuccess(
            """
            {"items":[],"returned":0,"total":null,"hasMore":false}
            """);
    }

    [Fact]
    public async Task List_Should_WriteSuggestionHint_When_FilteredResultHasAnUnknownKey()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSessionWithWorkspace();
        SetupListLogs();
        SetupListAttributeKeys(
            keys:
            [
                new AttributeKeyRow("Log", "http.response.status_code"),
                new AttributeKeyRow("Log", "http.status_code")
            ]);

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "logs",
            "list",
            "--filter",
            "http.statuscode:>=500");

        // assert
        result.AssertSuccess(
            """
            {"items":[],"returned":0,"total":null,"hasMore":false,"hint":"no results; unknown key \u0027http.statuscode\u0027, did you mean http.status_code, http.response.status_code? Run nitro telemetry attributes keys --signal logs to list keys."}
            """);
        TelemetryClientMock.Verify(
            x => x.ListAttributeKeysAsync(
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

    [Fact]
    public async Task List_Should_NotWriteSuggestionHint_When_FilterKeyIsKnown()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListLogs();
        SetupListAttributeKeys(keys: [new AttributeKeyRow("Log", "http.statuscode")]);

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "logs",
            "list",
            "--filter",
            "http.statuscode:>=500");

        // assert
        result.AssertSuccess(
            """
            {"items":[],"returned":0,"total":null,"hasMore":false}
            """);
    }

    [Fact]
    public async Task List_Should_ReturnOrdinaryEmptyResult_When_AttributeKeyLookupFails()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSessionWithWorkspace();
        SetupListLogs();
        TelemetryClientMock.Setup(x => x.ListAttributeKeysAsync(
                WorkspaceId,
                OpenTelemetrySignalKind.Logs,
                null,
                null,
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                50,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "logs",
            "list",
            "--filter",
            "http.statuscode:>=500");

        // assert
        result.AssertSuccess(
            """
            {"items":[],"returned":0,"total":null,"hasMore":false}
            """);
    }

    [Fact]
    public async Task Show_Should_WriteExceptionDetails_When_LogContainsAnException()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSessionWithWorkspace();
        SetupGetLog(CreateLog());

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "logs",
            "show",
            "log-1");

        // assert
        result.AssertSuccess(
            """
            2026-01-01 00:00:00.123 UTC ERROR products Request failed [CheckoutHandler.Handle src/CheckoutHandler.cs:42]
            code.function: CheckoutHandler.Handle
            code.filepath: src/CheckoutHandler.cs
            code.lineno: 42
            exception.type: System.TimeoutException
            exception.message: The operation timed out.
            exception.stacktrace: at CheckoutHandler.Handle()
               at Program.Main()
            service.name: products
            deployment.environment: production
            scope: OpenTelemetry.Instrumentation
            scope.version: 1.0.0
            scope.schema_url: https://opentelemetry.io/schemas/1.0.0
            scope.scope.attribute: scope-value
            trace id: trace-1
            span id: span-1
            """);
    }

    private void SetupListLogs(bool hasNextPage = false, params LogRow[] logs)
    {
        TelemetryClientMock.Setup(x => x.ListLogsAsync(
                WorkspaceId,
                It.IsAny<OpenTelemetryFilterInput?>(),
                It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                50,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<LogRow>(logs, null, hasNextPage));
    }

    private void SetupGetLog(Log? log)
    {
        TelemetryClientMock.Setup(x => x.GetLogAsync(
                WorkspaceId,
                "log-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);
    }

    private static LogRow CreateLogRow()
        => new(
            "log-1",
            1767225600123,
            "ERROR",
            17,
            "Request failed",
            "trace-1",
            "span-1",
            "products");

    private static bool IsWarnOrHigherFilter(OpenTelemetryFilterInput? filter)
    {
        var values = filter?.Attribute?.Condition.In;

        return filter?.Attribute?.Key == "severity"
            && values?.Count == 3
            && values?[0].String == "warn"
            && values?[1].String == "error"
            && values?[2].String == "fatal";
    }

    private static bool IsTraceIdFilter(OpenTelemetryFilterInput? filter)
        => filter?.Attribute?.Key == "trace.id"
            && filter?.Attribute?.Condition.Eq?.String == "trace-1";

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
