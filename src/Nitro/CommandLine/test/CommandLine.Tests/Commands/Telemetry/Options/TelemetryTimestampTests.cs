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

    [Theory]
    [InlineData("--since", true)]
    [InlineData("--until", false)]
    public void TryParse_Should_ReturnError_When_DurationOversizedUnderflows(
        string optionName,
        bool enforceMaximumAge)
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var success = TelemetryTimestamp.TryParse(
            "999999d",
            now,
            optionName,
            enforceMaximumAge,
            out _,
            out var error);

        // assert
        Assert.False(success);
        Assert.Equal(
            $"""
            Option '{optionName}' cannot be more than 60 days in the past.
            hint: choose a more recent timestamp or duration.
            """,
            error);
    }

    [Fact]
    public void Parse_Should_AddHintAndReturnExitCodeOne_When_SinceValueIsInvalid()
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
        Assert.Equal(1, result.Invoke());
        Assert.Equal(
            """
            Option '--since' received an invalid value: yesterday
            hint: use a duration such as 30m, 2h, or 7d, or an ISO 8601 timestamp.
            """,
            Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void Parse_Should_AddHintAndReturnExitCodeOne_When_SinceValueIsOlderThanSixtyDays()
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
        var result = command.Parse(["--since", "61d"]);

        // assert
        Assert.Equal(1, result.Invoke());
        Assert.Equal(
            """
            Option '--since' cannot be more than 60 days in the past.
            hint: choose a more recent timestamp or duration.
            """,
            Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void Defaults_Should_UseDefaultTimeRange_When_OptionsAreNotSpecified()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        using var provider = new ServiceCollection()
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
        command.Options.Add(new TelemetryTraceSearchOption());

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
            "--search", "checkout"
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
