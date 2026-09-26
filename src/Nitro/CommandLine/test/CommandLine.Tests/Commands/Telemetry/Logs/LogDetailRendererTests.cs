using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Logs;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Logs;

public sealed class LogDetailRendererTests
{
    [Fact]
    public void Render_Should_WriteEverySection_When_DetailIsComplete()
    {
        // arrange
        var detail = new ShowLogCommand.LogDetail(
            "log-1",
            1767225600123,
            "ERROR",
            17,
            "products",
            "Request failed",
            "trace-1",
            "span-1",
            new LogBodyDetail(null, "String", "Request failed"),
            [new("code.function", "CheckoutHandler.Handle"), new("exception.type", "System.TimeoutException")],
            [new("service.name", "products"), new("deployment.environment", "production")],
            new ShowLogCommand.LogScopeDetail(
                "OpenTelemetry.Instrumentation",
                "https://opentelemetry.io/schemas/1.0.0",
                "1.0.0",
                [new("scope.attribute", "scope-value")]),
            "CheckoutHandler.Handle",
            "src/CheckoutHandler.cs",
            42);
        var renderer = new LogDetailRenderer();

        // act
        var rendered = renderer.Render(detail);

        // assert
        rendered.MatchInlineSnapshot(
            """
            2026-01-01 00:00:00.123 UTC ERROR products Request failed [CheckoutHandler.Handle src/CheckoutHandler.cs:42]
            code.function: CheckoutHandler.Handle
            exception.type: System.TimeoutException
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

    [Fact]
    public void Render_Should_WriteHeaderAndIdsOnly_When_DetailIsMinimal()
    {
        // arrange
        var detail = new ShowLogCommand.LogDetail(
            "log-1",
            1767225600123,
            "INFO",
            9,
            "products",
            "Request completed",
            "trace-1",
            "span-1",
            new LogBodyDetail(null, "String", "Request completed"),
            [],
            [],
            null,
            null,
            null,
            null);
        var renderer = new LogDetailRenderer();

        // act
        var rendered = renderer.Render(detail);

        // assert
        rendered.MatchInlineSnapshot(
            """
            2026-01-01 00:00:00.123 UTC INFO products Request completed
            trace id: trace-1
            span id: span-1
            """);
    }
}
