using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Services;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Options;

public sealed class TelemetryTimestampTests
{
    [Theory]
    [InlineData("30m", "2026-01-01T11:30:00Z")]
    [InlineData("2h", "2026-01-01T10:00:00Z")]
    [InlineData("7d", "2025-12-25T12:00:00Z")]
    public void TryParse_Should_ParseDuration_When_ValueUsesSupportedUnit(
        string value,
        string expectedTimestamp)
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var success = TelemetryTimestamp.TryParse(
            value,
            now,
            "--since",
            enforceMaximumAge: true,
            out var timestamp,
            out var error);

        // assert
        Assert.True(success);
        Assert.Equal(DateTimeOffset.Parse(expectedTimestamp), timestamp);
        Assert.Null(error);
    }

    [Fact]
    public void TryParse_Should_ParseTimestamp_When_ValueIsIso8601()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var success = TelemetryTimestamp.TryParse(
            "2025-12-31T23:30:00Z",
            now,
            "--since",
            enforceMaximumAge: true,
            out var timestamp,
            out var error);

        // assert
        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero), timestamp);
        Assert.Null(error);
    }

    [Fact]
    public void TryParse_Should_ReturnHint_When_ValueIsInvalid()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var success = TelemetryTimestamp.TryParse(
            "yesterday",
            now,
            "--since",
            enforceMaximumAge: true,
            out _,
            out var error);

        // assert
        Assert.False(success);
        Assert.Equal(
            """
            Option '--since' received an invalid value: yesterday
            hint: use a duration such as 30m, 2h, or 7d, or an ISO 8601 timestamp.
            """,
            error);
    }

    [Fact]
    public void TryParse_Should_ReturnHint_When_SinceIsOlderThanSixtyDays()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var success = TelemetryTimestamp.TryParse(
            "61d",
            now,
            "--since",
            enforceMaximumAge: true,
            out _,
            out var error);

        // assert
        Assert.False(success);
        Assert.Equal(
            """
            Option '--since' cannot be more than 60 days in the past.
            hint: choose a more recent timestamp or duration.
            """,
            error);
    }

    [Fact]
    public void Parse_Should_AddHint_When_SinceValueIsInvalid()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var environmentVariables = new Mock<IEnvironmentVariableProvider>();
        using var provider = new ServiceCollection()
            .AddSingleton<IEnvironmentVariableProvider>(environmentVariables.Object)
            .AddSingleton<TimeProvider>(new FakeTimeProvider(now))
            .BuildServiceProvider();
        CommandExecutionContext.Initialize(new CommandServices(provider));
        var since = new TelemetrySinceOption();
        var command = new Command("telemetry");
        command.Options.Add(since);

        // act
        var result = command.Parse(["--since", "yesterday"]);

        // assert
        Assert.Equal(
            """
            Option '--since' received an invalid value: yesterday
            hint: use a duration such as 30m, 2h, or 7d, or an ISO 8601 timestamp.
            """,
            Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void Defaults_Should_UseDefaultTimeRange_When_EnvironmentVariablesAreNotSpecified()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var environmentVariables = new Mock<IEnvironmentVariableProvider>();
        using var provider = new ServiceCollection()
            .AddSingleton<IEnvironmentVariableProvider>(environmentVariables.Object)
            .AddSingleton<TimeProvider>(new FakeTimeProvider(now))
            .BuildServiceProvider();
        CommandExecutionContext.Initialize(new CommandServices(provider));
        var since = new TelemetrySinceOption();
        var until = new TelemetryUntilOption();
        var command = new Command("telemetry");
        command.Options.Add(since);
        command.Options.Add(until);

        // act
        var result = command.Parse([]);

        // assert
        Assert.Equal(now - TimeSpan.FromMinutes(30), result.GetValue(since));
        Assert.Equal(now, result.GetValue(until));
    }

    [Fact]
    public void Defaults_Should_ReadEnvironmentVariables_When_OptionsAreNotSpecified()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var environmentVariables = new Mock<IEnvironmentVariableProvider>();
        environmentVariables.Setup(x => x.GetEnvironmentVariable("NITRO_SINCE")).Returns("2h");
        environmentVariables.Setup(x => x.GetEnvironmentVariable("NITRO_UNTIL")).Returns("2026-01-01T11:30:00Z");
        environmentVariables.Setup(x => x.GetEnvironmentVariable("NITRO_SERVICE")).Returns("orders");
        environmentVariables.Setup(x => x.GetEnvironmentVariable("NITRO_ENV")).Returns("production");
        environmentVariables.Setup(x => x.GetEnvironmentVariable("NITRO_LIMIT")).Returns("42");
        using var provider = new ServiceCollection()
            .AddSingleton<IEnvironmentVariableProvider>(environmentVariables.Object)
            .AddSingleton<TimeProvider>(new FakeTimeProvider(now))
            .BuildServiceProvider();
        CommandExecutionContext.Initialize(new CommandServices(provider));
        var service = new TelemetryServiceOption();
        var environment = new TelemetryEnvironmentOption();
        var since = new TelemetrySinceOption();
        var until = new TelemetryUntilOption();
        var limit = new TelemetryLimitOption();
        var command = new Command("telemetry");
        command.Options.Add(service);
        command.Options.Add(environment);
        command.Options.Add(since);
        command.Options.Add(until);
        command.Options.Add(limit);

        // act
        var result = command.Parse([]);

        // assert
        Assert.Equal(now - TimeSpan.FromHours(2), result.GetValue(since));
        Assert.Equal(now - TimeSpan.FromMinutes(30), result.GetValue(until));
        Assert.Equal("orders", result.GetValue(service));
        Assert.Equal(["production"], result.GetValue(environment) ?? []);
        Assert.Equal(42, result.GetValue(limit));
    }

    [Fact]
    public void Parse_Should_AcceptTelemetryOptions_When_ValuesAreValid()
    {
        // arrange
        using var provider = InitializeCommandServices();
        var command = new Command("telemetry");
        command.Options.Add(new TelemetryServiceOption());
        command.Options.Add(new TelemetryEnvironmentOption());
        command.Options.Add(new TelemetrySinceOption());
        command.Options.Add(new TelemetryUntilOption());
        command.Options.Add(new TelemetryLimitOption());
        command.Options.Add(new TelemetryFilterOption());
        command.Options.Add(new TelemetryHasErrorOption());
        command.Options.Add(new TelemetryMinDurationOption());
        command.Options.Add(new TelemetrySpanKindOption());
        command.Options.Add(new TelemetrySeverityOption());
        command.Options.Add(new TelemetryTraceIdOption());
        command.Options.Add(new TelemetrySearchOption());
        command.Options.Add(new TelemetryOutputFormatOption());

        // act
        var result = command.Parse(
        [
            "--service", "orders",
            "--env", "production",
            "--env", "staging",
            "--since", "30m",
            "--until", "2026-01-01T12:00:00Z",
            "--limit", "20",
            "--filter", "status:error",
            "--has-error",
            "--min-duration", "10",
            "--span-kind", "SERVER",
            "--span-kind", "CLIENT",
            "--severity", "warn",
            "--trace-id", "trace-1",
            "--search", "checkout",
            "--output", "ndjson"
        ]);

        // assert
        Assert.Empty(result.Errors);
    }

    private static ServiceProvider InitializeCommandServices()
    {
        var provider = new ServiceCollection()
            .AddSingleton<IEnvironmentVariableProvider>(Mock.Of<IEnvironmentVariableProvider>())
            .AddSingleton<TimeProvider>(TimeProvider.System)
            .BuildServiceProvider();
        CommandExecutionContext.Initialize(new CommandServices(provider));

        return provider;
    }
}
