using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas.Analytics;

public sealed class UnusedSchemaCommandTests(NitroCommandFixture fixture)
    : AnalyticsCommandTestBase(fixture)
{
    [Fact]
    public async Task Unused_Markdown_ReturnsSuccess()
    {
        // arrange
        SetupUnusedCoordinates();

        // act
        var result = await ExecuteCommandAsync(
            "schema", "unused",
            "--api-id", ApiId,
            "--stage", Stage,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "markdown");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            ---
            api: api-1
            stage: dev
            window: 2026-04-06 to 2026-04-13
            limit: 100
            ---

            ## Unused coordinates (2)

            | Coordinate | Deprecated | Last seen |
            |---|---|---|
            | Review.legacyScore | no | 2025-12-01 |
            | User.legacyName | yes | 2025-10-14 |
            """);
    }

    [Fact]
    public async Task Unused_Json_ReturnsSuccess()
    {
        // arrange
        SetupUnusedCoordinates();

        // act
        var result = await ExecuteCommandAsync(
            "schema", "unused",
            "--api-id", ApiId,
            "--stage", Stage,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "json");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("Review.legacyScore", result.StdOut);
        Assert.Contains("User.legacyName", result.StdOut);
        Assert.Contains("\"limit\": 100", result.StdOut);
    }

    [Fact]
    public async Task Unused_Table_ReturnsSuccess()
    {
        // arrange
        SetupUnusedCoordinates();

        // act
        var result = await ExecuteCommandAsync(
            "schema", "unused",
            "--api-id", ApiId,
            "--stage", Stage,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "table");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("Review.legacyScore", result.StdOut);
        Assert.Contains("User.legacyName", result.StdOut);
    }

    private void SetupUnusedCoordinates()
    {
        var unusedNoDeprecation =
            new UnusedCoordinatesQuery_ApiById_Stages_Coordinates_Nodes_GraphQLObjectFieldDefinition(
                coordinate: "Review.legacyScore",
                isDeprecated: false,
                usage:
                new UnusedCoordinatesQuery_ApiById_Stages_Coordinates_Nodes_Usage_CoordinateUsage(
                    totalRequests: 0L,
                    clientCount: 0L,
                    operationCount: 0L,
                    lastSeen: new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero)));

        var unusedDeprecated =
            new UnusedCoordinatesQuery_ApiById_Stages_Coordinates_Nodes_GraphQLObjectFieldDefinition(
                coordinate: "User.legacyName",
                isDeprecated: true,
                usage:
                new UnusedCoordinatesQuery_ApiById_Stages_Coordinates_Nodes_Usage_CoordinateUsage(
                    totalRequests: 0L,
                    clientCount: 0L,
                    operationCount: 0L,
                    lastSeen: new DateTimeOffset(2025, 10, 14, 0, 0, 0, TimeSpan.Zero)));

        var connection = new UnusedCoordinatesQuery_ApiById_Stages_Coordinates_CoordinatesConnection(
            new IUnusedCoordinatesQuery_ApiById_Stages_Coordinates_Nodes[]
            {
                unusedNoDeprecation,
                unusedDeprecated
            });

        var stage = new UnusedCoordinatesQuery_ApiById_Stages_Stage(
            name: Stage,
            coordinates: connection);

        CoordinatesClientMock
            .Setup(x => x.GetUnusedCoordinatesAsync(
                ApiId,
                Stage,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<IReadOnlyList<CoordinateKind>?>(),
                It.IsAny<bool?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
    }
}
