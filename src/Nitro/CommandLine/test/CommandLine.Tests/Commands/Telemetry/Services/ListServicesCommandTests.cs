using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Services;

public sealed class ListServicesCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_Should_ReturnSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List telemetry services in the current workspace.

            Usage:
              nitro telemetry services list [options]

            Options:
              --search <search>              Search service names
              --env <env>                    Limit results to an environment; can be used multiple times
              --filter <filter>              Filter results using the telemetry filter grammar
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
              nitro telemetry services list
              nitro telemetry services list --filter "status:error"
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
        var result = await ExecuteCommandAsync("telemetry", "services", "list");

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
        var result = await ExecuteCommandAsync("telemetry", "services", "list");

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            """);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_SinceIsInvalid()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list", "--since", "yesterday");

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
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list", "--until", "yesterday");

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
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list", "--since", "61d");

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
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list", "--since", "1h", "--until", "2h");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--since' must be earlier than '--until'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_LimitIsNotPositive()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list", "--limit", "0");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--limit' must be a positive number.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_RenderFilterDiagnostic_When_FilterIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list", "--filter", "status:");

        // assert
        result.AssertError(
            """
            filter: Missing value in key:value pair at column 7
            status:
                  ^
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReturnSuccess_When_ServicesExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListServices(services: [CreateService()]);

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "name": "products",
                  "environments": "production, staging",
                  "lastVersion": "1.2.0"
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
    public async Task List_Should_ReturnSuccess_When_NoServicesExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListServices();

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list");

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
    public async Task List_Should_ReturnCursor_When_ResultHasMoreItems(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListServices(hasNextPage: true, services: [CreateService()]);

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "name": "products",
                  "environments": "production, staging",
                  "lastVersion": "1.2.0"
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
    public async Task List_Should_ForwardSearchEnvironmentsAndFilter_When_OptionsAreSpecified(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListServices(
            search: "prod",
            environments: ["production", "staging"],
            filterPredicate: filter =>
                filter?.Attribute?.Key == "status" && filter.Attribute.Condition.Eq?.String == "error");

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "services",
            "list",
            "--search",
            "prod",
            "--env",
            "production",
            "--env",
            "staging",
            "--filter",
            "status:error");

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
        SetupListServices(filterPredicate: _ => true);

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list", "--filter", "http.statuscode:>=500");

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
        SetupListServicesException();

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list");

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
                x.ListServicesAsync(
                    WorkspaceId,
                    null,
                    null,
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>(),
                    50,
                    cursor,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<ServiceRow>([CreateService()], "cursor-2", true));

        // act
        var result = await ExecuteCommandAsync(["telemetry", "services", "list", .. cursorArguments]);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "name": "products",
                  "environments": "production, staging",
                  "lastVersion": "1.2.0"
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    private void SetupListServices(
        string? search = null,
        string[]? environments = null,
        Func<OpenTelemetryFilterInput?, bool>? filterPredicate = null,
        bool hasNextPage = false,
        params ServiceRow[] services)
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListServicesAsync(
                    WorkspaceId,
                    search,
                    It.Is<OpenTelemetryFilterInput?>(filter => MatchesFilter(filter, filterPredicate)),
                    It.Is<IReadOnlyList<string>?>(actual => MatchesEnvironments(actual, environments)),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>(),
                    50,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<ServiceRow>(services, hasNextPage ? "cursor-2" : null, hasNextPage));
    }

    private void SetupListServicesException()
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListServicesAsync(
                    WorkspaceId,
                    It.IsAny<string?>(),
                    It.IsAny<OpenTelemetryFilterInput?>(),
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>(),
                    50,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    private static bool MatchesFilter(
        OpenTelemetryFilterInput? filter,
        Func<OpenTelemetryFilterInput?, bool>? filterPredicate)
        => filterPredicate?.Invoke(filter) ?? filter is null;

    private static bool MatchesEnvironments(IReadOnlyList<string>? actual, string[]? expected)
        => (actual ?? []).SequenceEqual(expected ?? []);

    private static ServiceRow CreateService()
        => new(
            "products",
            ["production", "staging"],
            [
                new ServiceVersionMarker(new DateTimeOffset(2025, 12, 31, 22, 0, 0, TimeSpan.Zero), "1.0.0"),
                new ServiceVersionMarker(new DateTimeOffset(2025, 12, 31, 23, 0, 0, TimeSpan.Zero), "1.2.0"),
                new ServiceVersionMarker(new DateTimeOffset(2025, 12, 31, 22, 30, 0, TimeSpan.Zero), "1.1.0")
            ]);
}
