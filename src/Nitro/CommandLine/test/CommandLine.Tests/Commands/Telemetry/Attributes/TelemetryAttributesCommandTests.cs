using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Attributes;

public sealed class TelemetryAttributesCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    [Fact]
    public async Task KeysHelp_Should_ReturnSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "keys", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List telemetry attribute keys.

            Usage:
              nitro telemetry attributes keys [options]

            Options:
              --signal <logs|traces> (REQUIRED)  The telemetry signal to inspect
              --kind <kind>                      Limit results to an attribute kind; can be used multiple times
              --search <search>                  Search attribute keys
              --since <since>                    The earliest timestamp to include [default: 12/31/2025 23:30:00 +00:00]
              --until <until>                    The latest timestamp to include [default: 01/01/2026 00:00:00 +00:00]
              --limit <limit>                    The maximum number of results to show
              --cloud-url <cloud-url>            The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                    The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                     Show help and usage information

            Example:
              nitro telemetry attributes keys --signal traces
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Keys_Should_ReturnError_When_WorkspaceIsUnavailable(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSession();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "keys", "--signal", "traces");

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            hint: run `nitro workspace set-default`.
            """);
    }

    [Fact]
    public async Task Keys_Should_WriteEnvelopeWithHint_When_ResultHasMoreItems()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListAttributeKeys(hasNextPage: true, keys: [new AttributeKeyRow("Resource", "service.name")]);

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "keys", "--signal", "traces");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [
                {
                  "key": "service.name",
                  "kind": "Resource"
                }
              ],
              "returned": 1,
              "total": null,
              "hasMore": true,
              "hint": "showing 1 (more), narrow with --since, or raise --limit"
            }
            """);
    }

    [Fact]
    public async Task Values_Should_WriteEnvelope_When_ValuesExist()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupListAttributeValues(
            new AttributeValue(null, null, null, "products"),
            new AttributeValue(null, null, 42, null),
            new AttributeValue(null, 1.5, null, null),
            new AttributeValue(true, null, null, null),
            new AttributeValue(false, null, null, null));

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", "service.name", "--signal", "logs");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [
                {
                  "value": "products"
                },
                {
                  "value": "42"
                },
                {
                  "value": "1.5"
                },
                {
                  "value": "true"
                },
                {
                  "value": "false"
                }
              ],
              "returned": 5,
              "total": null,
              "hasMore": false
            }
            """);
    }

    private void SetupListAttributeValues(params AttributeValue[] values)
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListAttributeValuesAsync(
                    WorkspaceId,
                    It.IsAny<OpenTelemetrySignalKind>(),
                    "service.name",
                    null,
                    It.IsAny<string?>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(),
                    50,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<AttributeValue>(values, null, false));
    }
}
