using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas.Analytics;

public sealed class UsageSchemaCommandTests(NitroCommandFixture fixture)
    : AnalyticsCommandTestBase(fixture)
{
    private const string Coordinate = "User.email";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("schema", "usage", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("schema usage", result.StdOut);
        Assert.Contains("--coordinate", result.StdOut);
        Assert.Contains("--format", result.StdOut);
    }

    [Fact]
    public async Task Usage_SingleCoordinate_Markdown_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateUsage(Coordinate);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "usage",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", Coordinate,
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
            coordinate: User.email
            window: 2026-04-06 to 2026-04-13
            ---

            | Metric | Value |
            |---|---|
            | Total requests | 4,218,704 |
            | Clients | 7 |
            | Operations | 23 |
            | First seen | 2024-11-02 |
            | Last seen | 2026-04-13 |
            | Error rate | 0.03% |
            | Mean duration | 42.1ms |
            """);
    }

    [Fact]
    public async Task Usage_SingleCoordinate_Json_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateUsage(Coordinate);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "usage",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", Coordinate,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "json");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "version": 1,
              "api": "api-1",
              "stage": "dev",
              "window": {
                "from": "2026-04-06T00:00:00+00:00",
                "to": "2026-04-13T00:00:00+00:00"
              },
              "data": {
                "coordinates": {
                  "User.email": {
                    "coordinate": "User.email",
                    "isDeprecated": false,
                    "totalRequests": 4218704,
                    "clientCount": 7,
                    "operationCount": 23,
                    "firstSeen": "2024-11-02T00:00:00+00:00",
                    "lastSeen": "2026-04-13T00:00:00+00:00",
                    "errorRate": 0.0003,
                    "meanDuration": 42.1
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Usage_SingleCoordinate_Table_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateUsage(Coordinate);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "usage",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", Coordinate,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "table");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("User.email", result.StdOut);
        Assert.Contains("Total requests", result.StdOut);
        Assert.Contains("4,218,704", result.StdOut);
        Assert.Contains("Clients", result.StdOut);
        Assert.Contains("7", result.StdOut);
        Assert.Contains("23", result.StdOut);
    }

    private void SetupCoordinateUsage(string coordinate)
    {
        var usage = new CoordinateUsageQuery_ApiById_Stages_Coordinate_Usage_CoordinateUsage(
            totalRequests: 4_218_704L,
            clientCount: 7L,
            operationCount: 23L,
            firstSeen: new DateTimeOffset(2024, 11, 2, 0, 0, 0, TimeSpan.Zero),
            lastSeen: new DateTimeOffset(2026, 4, 13, 0, 0, 0, TimeSpan.Zero),
            errorRate: 0.0003,
            meanDuration: 42.1);

        var coordinateData = new CoordinateUsageQuery_ApiById_Stages_Coordinate_GraphQLObjectFieldDefinition(
            coordinate: coordinate,
            isDeprecated: false,
            usage: usage);

        var stage = new CoordinateUsageQuery_ApiById_Stages_Stage(
            name: Stage,
            coordinate: coordinateData);

        CoordinatesClientMock
            .Setup(x => x.GetCoordinateUsageAsync(
                ApiId,
                Stage,
                coordinate,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
    }
}
