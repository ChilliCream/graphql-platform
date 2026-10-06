using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Attributes;

public sealed class ListAttributeValuesCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    private const string Key = "service.name";

    private static readonly DateTimeOffset s_defaultSince = new(2025, 12, 31, 23, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset s_defaultUntil = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Help_Should_ReturnSuccess()
    {
        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List telemetry attribute values.

            Usage:
              nitro telemetry attributes values <key> [options]

            Arguments:
              <key>  The attribute key

            Options:
              --signal <logs|traces> (REQUIRED)                              The telemetry signal to inspect
              --kind <Body|Event|Field|Link|Log|Metric|Resource|Scope|Span>  Limit results to an attribute kind
              --search <search>                                              Search attribute values
              --since <since>                                                The earliest timestamp to include [default: 12/31/2025 23:30:00 +00:00]
              --until <until>                                                The latest timestamp to include [default: 01/01/2026 00:00:00 +00:00]
              --limit <limit>                                                The maximum number of results to show
              --cursor <cursor>                                              The pagination cursor to resume from [env: NITRO_CURSOR]
              --workspace-id <workspace-id>                                  The ID of the workspace [env: NITRO_WORKSPACE_ID]
              --cloud-url <cloud-url>                                        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                                            The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                                                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                                 Show help and usage information

            Example:
              nitro telemetry attributes values service.name --signal traces
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Values_Should_ReturnError_When_AuthenticationIsUnavailable(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", Key, "--signal", "traces");

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
    public async Task Values_Should_ReturnError_When_WorkspaceIsUnavailable(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSession();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", Key, "--signal", "traces");

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            """);
    }

    [Fact]
    public async Task Values_Should_ReturnError_When_KeyIsMissing()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", "--signal", "traces");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Required argument missing for command: 'values'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Values_Should_ReturnError_When_SignalIsMissing()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", Key);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--signal' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Values_Should_ReturnError_When_SignalIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", Key, "--signal", "metrics");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Argument 'metrics' not recognized. Must be one of:
                'traces'
                'logs'
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Values_Should_ReturnError_When_SinceIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "values",
            Key,
            "--signal",
            "traces",
            "--since",
            "yesterday");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--since' received an invalid value: yesterday
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Values_Should_ReturnError_When_UntilIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "values",
            Key,
            "--signal",
            "traces",
            "--until",
            "yesterday");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--until' received an invalid value: yesterday
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Values_Should_ReturnError_When_SinceIsOlderThanSixtyDays()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "values",
            Key,
            "--signal",
            "traces",
            "--since",
            "61d");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--since' cannot be more than 60 days in the past.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Values_Should_ReturnError_When_SinceIsNotEarlierThanUntil()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "values",
            Key,
            "--signal",
            "traces",
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

    [Fact]
    public async Task Values_Should_ReturnError_When_LimitIsZero()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "values",
            Key,
            "--signal",
            "traces",
            "--limit",
            "0");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--limit' must be a positive number.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Values_Should_ReturnError_When_KindIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "values",
            Key,
            "--signal",
            "traces",
            "--kind",
            "bogus");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Cannot parse argument 'bogus' for option '--kind' as expected type 'ChilliCream.Nitro.Client.OpenTelemetryAttributeKind'. Did you mean one of the following?
            Body
            Event
            Field
            Link
            Log
            Metric
            Resource
            Scope
            Span
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Values_Should_ReturnPage_When_ValuesExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListAttributeValuesPage(
            OpenTelemetrySignalKind.Logs,
            OpenTelemetryAttributeKind.Resource,
            "prod",
            new DateTimeOffset(2025, 12, 31, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 12, 31, 23, 50, 0, TimeSpan.Zero),
            5,
            hasNextPage: false,
            new AttributeValue(null, null, null, "products"),
            new AttributeValue(null, null, null, "orders"));

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "values",
            Key,
            "--signal",
            "logs",
            "--kind",
            "Resource",
            "--search",
            "prod",
            "--since",
            "1h",
            "--until",
            "10m",
            "--limit",
            "5");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "value": "products"
                },
                {
                  "value": "orders"
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
    public async Task Values_Should_WriteTypedValues_When_ValuesHaveDifferentTypes(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListAttributeValuesPage(
            OpenTelemetrySignalKind.Logs,
            hasNextPage: false,
            new AttributeValue(null, null, null, "products"),
            new AttributeValue(null, null, 42, null),
            new AttributeValue(null, 1.5, null, null),
            new AttributeValue(true, null, null, null),
            new AttributeValue(false, null, null, null));

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", Key, "--signal", "logs");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
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
              "cursor": null
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Values_Should_ReturnEmptyPage_When_NoValuesExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListAttributeValuesPage(OpenTelemetrySignalKind.Traces, hasNextPage: false);

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", Key, "--signal", "traces");

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
    public async Task Values_Should_ReturnCursor_When_MoreValuesExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListAttributeValuesPage(
            OpenTelemetrySignalKind.Traces,
            hasNextPage: true,
            new AttributeValue(null, null, null, "products"));

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", Key, "--signal", "traces");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "value": "products"
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
    public async Task Values_Should_ReturnError_When_ClientThrows(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        TelemetryClientMock
            .Setup(x =>
                x.ListAttributeValuesAsync(
                    WorkspaceId,
                    OpenTelemetrySignalKind.Traces,
                    Key,
                    null,
                    null,
                    s_defaultSince,
                    s_defaultUntil,
                    50,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "values", Key, "--signal", "traces");

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
    public async Task Values_Should_ResumeFromCursor_When_CursorIsSpecified(
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
                x.ListAttributeValuesAsync(
                    WorkspaceId,
                    OpenTelemetrySignalKind.Traces,
                    Key,
                    null,
                    null,
                    s_defaultSince,
                    s_defaultUntil,
                    50,
                    cursor,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<AttributeValue>([new AttributeValue(null, null, null, "products")], "cursor-2", true));

        // act
        var result = await ExecuteCommandAsync(["telemetry", "attributes", "values", Key, "--signal", "traces", .. cursorArguments]);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "value": "products"
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    private void SetupListAttributeValuesPage(
        OpenTelemetrySignalKind signal,
        bool hasNextPage,
        params AttributeValue[] values)
    {
        SetupListAttributeValuesPage(signal, null, null, s_defaultSince, s_defaultUntil, 50, hasNextPage, values);
    }

    private void SetupListAttributeValuesPage(
        OpenTelemetrySignalKind signal,
        OpenTelemetryAttributeKind? kind,
        string? search,
        DateTimeOffset since,
        DateTimeOffset until,
        int limit,
        bool hasNextPage,
        params AttributeValue[] values)
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListAttributeValuesAsync(
                    WorkspaceId,
                    signal,
                    Key,
                    kind,
                    search,
                    since,
                    until,
                    limit,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<AttributeValue>(values, hasNextPage ? "cursor-2" : null, hasNextPage));
    }
}
