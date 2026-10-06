using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Services;

public sealed class ShowServiceCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    private const string ServiceName = "products";

    [Fact]
    public async Task Help_Should_ReturnSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "show", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show a telemetry service.

            Usage:
              nitro telemetry services show <name> [options]

            Arguments:
              <name>  The service name

            Options:
              --env <env>                    Limit results to an environment; can be used multiple times
              --since <since>                The earliest timestamp to include [default: 12/31/2025 23:30:00 +00:00]
              --until <until>                The latest timestamp to include [default: 01/01/2026 00:00:00 +00:00]
              --workspace-id <workspace-id>  The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>            The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                 Show help and usage information

            Example:
              nitro telemetry services show "<name>"
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
        var result = await ExecuteCommandAsync("telemetry", "services", "show", ServiceName);

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
        var result = await ExecuteCommandAsync("telemetry", "services", "show", ServiceName);

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            """);
    }

    [Fact]
    public async Task Show_Should_ReturnError_When_ServiceNameIsMissing()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "show");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Required argument missing for command: 'show'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Show_Should_ReturnError_When_SinceIsInvalid()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "show", ServiceName, "--since", "yesterday");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--since' received an invalid value: yesterday
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Show_Should_ReturnError_When_UntilIsInvalid()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "show", ServiceName, "--until", "yesterday");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--until' received an invalid value: yesterday
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Show_Should_ReturnError_When_SinceIsOlderThanSixtyDays()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "show", ServiceName, "--since", "61d");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--since' cannot be more than 60 days in the past.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Show_Should_ReturnError_When_SinceIsNotEarlierThanUntil()
    {
        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "services",
            "show",
            ServiceName,
            "--since",
            "1h",
            "--until",
            "2h");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--since' must be earlier than '--until'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_WriteServiceDetail_When_ServiceExists(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupGetService(CreateService());

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "show", ServiceName);

        // assert
        result.AssertSuccess(
            """
            {
              "name": "products",
              "environments": [
                "production",
                "staging"
              ],
              "versionMarkers": [
                {
                  "version": "1.0.0",
                  "firstSeenAt": "2025-12-31T22:00:00+00:00"
                },
                {
                  "version": "1.2.0",
                  "firstSeenAt": "2025-12-31T23:00:00+00:00"
                },
                {
                  "version": "1.1.0",
                  "firstSeenAt": "2025-12-31T22:30:00+00:00"
                }
              ]
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_ForwardEnvironments_When_EnvIsSpecified(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupGetService(CreateService(), environments: ["production", "staging"]);

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "services",
            "show",
            ServiceName,
            "--env",
            "production",
            "--env",
            "staging");

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_ReturnError_When_ServiceDoesNotExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupGetService(null);

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "show", ServiceName);

        // assert
        result.AssertError(
            """
            The service 'products' was not found.
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
        SetupGetServiceException();

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "show", ServiceName);

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_PrintLiteralName_When_MissingServiceNameContainsMarkup(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        TelemetryClientMock
            .Setup(x => x.GetServiceAsync(
                WorkspaceId,
                "[products]",
                It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceRow?)null);

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "show", "[products]");

        // assert
        result.AssertError("The service '[products]' was not found.");
    }

    private void SetupGetService(ServiceRow? service, string[]? environments = null)
    {
        TelemetryClientMock
            .Setup(x =>
                x.GetServiceAsync(
                    WorkspaceId,
                    ServiceName,
                    It.Is<IReadOnlyList<string>?>(actual => MatchesEnvironments(actual, environments)),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(service);
    }

    private void SetupGetServiceException()
    {
        TelemetryClientMock
            .Setup(x =>
                x.GetServiceAsync(
                    WorkspaceId,
                    ServiceName,
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    private static bool MatchesEnvironments(IReadOnlyList<string>? actual, string[]? expected)
        => (actual ?? []).SequenceEqual(expected ?? []);

    private static ServiceRow CreateService()
        => new(
            ServiceName,
            ["production", "staging"],
            [
                new ServiceVersionMarker(new DateTimeOffset(2025, 12, 31, 22, 0, 0, TimeSpan.Zero), "1.0.0"),
                new ServiceVersionMarker(new DateTimeOffset(2025, 12, 31, 23, 0, 0, TimeSpan.Zero), "1.2.0"),
                new ServiceVersionMarker(new DateTimeOffset(2025, 12, 31, 22, 30, 0, TimeSpan.Zero), "1.1.0")
            ]);
}
