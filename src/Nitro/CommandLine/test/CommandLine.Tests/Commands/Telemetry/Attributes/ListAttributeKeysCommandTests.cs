using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Attributes;

public sealed class ListAttributeKeysCommandTests(NitroCommandFixture fixture) : TelemetryCommandTestBase(fixture)
{
    private static readonly DateTimeOffset s_defaultSince = new(2025, 12, 31, 23, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset s_defaultUntil = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Help_Should_ReturnSuccess()
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
              --cursor <cursor>                  The pagination cursor to resume from [env: NITRO_CURSOR]
              --workspace-id <workspace-id>      The ID of the workspace [env: NITRO_WORKSPACE_ID]
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
    public async Task Keys_Should_ReturnError_When_AuthenticationIsUnavailable(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "keys", "--signal", "traces");

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
            """);
    }

    [Fact]
    public async Task Keys_Should_ReturnError_When_SignalIsMissing()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "keys");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--signal' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Keys_Should_ReturnError_When_SignalIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "keys", "--signal", "metrics");

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
    public async Task Keys_Should_ReturnError_When_SinceIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "keys",
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
    public async Task Keys_Should_ReturnError_When_UntilIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "keys",
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
    public async Task Keys_Should_ReturnError_When_SinceIsOlderThanSixtyDays()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "keys",
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
    public async Task Keys_Should_ReturnError_When_SinceIsNotEarlierThanUntil()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "keys",
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
    public async Task Keys_Should_ReturnError_When_LimitIsZero()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "keys", "--signal", "traces", "--limit", "0");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--limit' must be a positive number.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Keys_Should_ReturnError_When_KindIsInvalid()
    {
        // arrange
        SetupSessionWithWorkspace();

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "keys",
            "--signal",
            "traces",
            "--kind",
            "bogus");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Cannot parse argument 'bogus' for option '--kind' as expected type 'ChilliCream.Nitro.Client.OpenTelemetryAttributeKind'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Keys_Should_ReturnPage_When_KeysExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListAttributeKeysPage(
            OpenTelemetrySignalKind.Logs,
            [OpenTelemetryAttributeKind.Resource, OpenTelemetryAttributeKind.Span],
            "service",
            new DateTimeOffset(2025, 12, 31, 23, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 12, 31, 23, 50, 0, TimeSpan.Zero),
            5,
            hasNextPage: false,
            new AttributeKeyRow("Resource", "service.name"),
            new AttributeKeyRow("Span", "service.version"));

        // act
        var result = await ExecuteCommandAsync(
            "telemetry",
            "attributes",
            "keys",
            "--signal",
            "logs",
            "--kind",
            "Resource",
            "--kind",
            "Span",
            "--search",
            "service",
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
                  "key": "service.name",
                  "kind": "Resource"
                },
                {
                  "key": "service.version",
                  "kind": "Span"
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
    public async Task Keys_Should_ReturnEmptyPage_When_NoKeysExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListAttributeKeysPage(OpenTelemetrySignalKind.Traces, hasNextPage: false);

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "keys", "--signal", "traces");

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
    public async Task Keys_Should_ReturnCursor_When_MoreKeysExist(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListAttributeKeysPage(
            OpenTelemetrySignalKind.Traces,
            hasNextPage: true,
            new AttributeKeyRow("Resource", "service.name"));

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "keys", "--signal", "traces");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "key": "service.name",
                  "kind": "Resource"
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
    public async Task Keys_Should_ReturnError_When_ClientThrows(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupSessionWithWorkspace();
        SetupListAttributeKeysException();

        // act
        var result = await ExecuteCommandAsync("telemetry", "attributes", "keys", "--signal", "traces");

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
    public async Task Keys_Should_ResumeFromCursor_When_CursorIsSpecified(
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
                x.ListAttributeKeysAsync(
                    WorkspaceId,
                    OpenTelemetrySignalKind.Traces,
                    It.IsAny<IReadOnlyList<OpenTelemetryAttributeKind>?>(),
                    null,
                    s_defaultSince,
                    s_defaultUntil,
                    50,
                    cursor,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<AttributeKeyRow>([new AttributeKeyRow("Resource", "service.name")], "cursor-2", true));

        // act
        var result = await ExecuteCommandAsync(["telemetry", "attributes", "keys", "--signal", "traces", .. cursorArguments]);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "key": "service.name",
                  "kind": "Resource"
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    private void SetupListAttributeKeysPage(
        OpenTelemetrySignalKind signal,
        bool hasNextPage,
        params AttributeKeyRow[] keys)
    {
        SetupListAttributeKeysPage(signal, [], null, s_defaultSince, s_defaultUntil, 50, hasNextPage, keys);
    }

    private void SetupListAttributeKeysPage(
        OpenTelemetrySignalKind signal,
        OpenTelemetryAttributeKind[] kinds,
        string? search,
        DateTimeOffset since,
        DateTimeOffset until,
        int limit,
        bool hasNextPage,
        params AttributeKeyRow[] keys)
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListAttributeKeysAsync(
                    WorkspaceId,
                    signal,
                    It.Is<IReadOnlyList<OpenTelemetryAttributeKind>?>(k => k != null && k.SequenceEqual(kinds)),
                    search,
                    since,
                    until,
                    limit,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<AttributeKeyRow>(keys, hasNextPage ? "cursor-2" : null, hasNextPage));
    }

    private void SetupListAttributeKeysException()
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListAttributeKeysAsync(
                    WorkspaceId,
                    OpenTelemetrySignalKind.Traces,
                    It.IsAny<IReadOnlyList<OpenTelemetryAttributeKind>?>(),
                    null,
                    s_defaultSince,
                    s_defaultUntil,
                    50,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }
}
