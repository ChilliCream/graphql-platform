using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Services;

public sealed class TelemetryServicesCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    private const string ServiceName = "products";

    [Fact]
    public async Task ListHelp_Should_ReturnSuccess()
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
              --search <search>        Search service names
              --env <env>              Limit results to an environment; can be used multiple times
              --filter <filter>        Filter results using the telemetry filter grammar
              --since <since>          The earliest timestamp to include [default: 12/31/2025 23:30:00 +00:00]
              --until <until>          The latest timestamp to include [default: 01/01/2026 00:00:00 +00:00]
              --limit <limit>          The maximum number of results to show
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>      The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro telemetry services list
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
            hint: run `nitro login`.
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
              "items": [
                {
                  "name": "products",
                  "environments": "production, staging",
                  "lastVersion": "1.2.0"
                }
              ],
              "returned": 1,
              "total": null,
              "hasMore": false
            }
            """);
    }

    [Fact]
    public async Task List_Should_NotAdvertiseServiceOption_When_ResultHasMoreItems()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListServices(hasNextPage: true, services: [CreateService()]);

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [
                {
                  "name": "products",
                  "environments": "production, staging",
                  "lastVersion": "1.2.0"
                }
              ],
              "returned": 1,
              "total": null,
              "hasMore": true,
              "hint": "showing 1 (more), narrow with --since or --filter, or raise --limit"
            }
            """);
    }

    [Fact]
    public async Task List_Should_ForwardCompiledFilter_When_FilterIsSpecified()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListServices(filter =>
            filter?.Attribute?.Key == "status" && filter.Attribute.Condition.Eq?.String == "error"
        );

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list", "--filter", "status:error");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [],
              "returned": 0,
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
        SetupListServices(_ => true);
        SetupListAttributeKeys(
            keys:
            [
                new AttributeKeyRow("Span", "http.response.status_code"),
                new AttributeKeyRow("Span", "http.status_code")
            ]);

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "list", "--filter", "http.statuscode:>=500");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [],
              "returned": 0,
              "total": null,
              "hasMore": false,
              "hint": "no results; unknown key \u0027http.statuscode\u0027, did you mean http.status_code, http.response.status_code? Run nitro telemetry attributes keys --signal traces to list keys."
            }
            """);
        TelemetryClientMock.Verify(
            x =>
                x.ListAttributeKeysAsync(
                    WorkspaceId,
                    OpenTelemetrySignalKind.Traces,
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
    public async Task Show_Should_ReturnError_When_ServiceDoesNotExist()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupGetService(null);

        // act
        var result = await ExecuteCommandAsync("telemetry", "services", "show", ServiceName);

        // assert
        result.AssertError(
            """
            The service 'products' was not found.
            hint: run nitro telemetry services list
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Show_Should_WriteServiceDetailAsJson_When_ServiceExists(InteractionMode mode)
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

    private void SetupListServices(
        Func<OpenTelemetryFilterInput?, bool>? filterPredicate = null,
        bool hasNextPage = false,
        params ServiceRow[] services)
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListServicesAsync(
                    WorkspaceId,
                    It.IsAny<string?>(),
                    It.Is<OpenTelemetryFilterInput?>(filter => MatchesFilter(filter, filterPredicate)),
                    It.IsAny<IReadOnlyList<string>?>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>(),
                    50,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<ServiceRow>(services, null, hasNextPage));
    }

    private static bool MatchesFilter(
        OpenTelemetryFilterInput? filter,
        Func<OpenTelemetryFilterInput?, bool>? filterPredicate)
        => filterPredicate?.Invoke(filter) ?? filter is null;

    private void SetupGetService(ServiceRow? service)
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
            .ReturnsAsync(service);
    }

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
