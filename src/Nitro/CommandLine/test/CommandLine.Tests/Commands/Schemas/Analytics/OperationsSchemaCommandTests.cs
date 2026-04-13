using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas.Analytics;

public sealed class OperationsSchemaCommandTests(NitroCommandFixture fixture)
    : AnalyticsCommandTestBase(fixture)
{
    private const string Coordinate = "User.email";

    [Fact]
    public async Task Operations_Markdown_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateOperations(Coordinate);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "operations",
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

            ## Operations (2)

            | Operation | Kind | Client | Requests | Error rate |
            |---|---|---|---|---|
            | GetCurrentUser | QUERY | web | 1,203,441 | 0% |
            | LoginFlow | QUERY | ios | 894,002 | 0.01% |
            """);
    }

    [Fact]
    public async Task Operations_Json_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateOperations(Coordinate);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "operations",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", Coordinate,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "json");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("GetCurrentUser", result.StdOut);
        Assert.Contains("LoginFlow", result.StdOut);
        Assert.Contains("1203441", result.StdOut);
    }

    [Fact]
    public async Task Operations_Table_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateOperations(Coordinate);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "operations",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", Coordinate,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "table");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("GetCurrentUser", result.StdOut);
        Assert.Contains("LoginFlow", result.StdOut);
        Assert.Contains("QUERY", result.StdOut);
        Assert.Contains("1,203,441", result.StdOut);
    }

    private void SetupCoordinateOperations(string coordinate)
    {
        var webOperations =
            new ICoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_Nodes[]
            {
                new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_Nodes_CoordinateClientUsageOperationInsight(
                    operationName: "GetCurrentUser",
                    hash: "hash-getcurrentuser",
                    kind: OperationKind.Query,
                    opm: 120.0,
                    totalCount: 1_203_441L,
                    totalCountWithErrors: 12L,
                    errorRate: 0.00001,
                    averageLatency: 42.1,
                    impact: 0.9,
                    totalVersions: 3L)
            };

        var iosOperations =
            new ICoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_Nodes[]
            {
                new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_Nodes_CoordinateClientUsageOperationInsight(
                    operationName: "LoginFlow",
                    hash: "hash-loginflow",
                    kind: OperationKind.Query,
                    opm: 80.0,
                    totalCount: 894_002L,
                    totalCountWithErrors: 80L,
                    errorRate: 0.0001,
                    averageLatency: 38.4,
                    impact: 0.7,
                    totalVersions: 2L)
            };

        var webClientUsage =
            new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_CoordinateClientUsage(
                name: "web",
                client: new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Client_Client(
                    "client-web",
                    "web"),
                metrics:
                new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_CoordinateClientUsageMetrics(
                    new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_CoordinateClientUsageOperationInsightsConnection(
                        webOperations)));

        var iosClientUsage =
            new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_CoordinateClientUsage(
                name: "ios",
                client: new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Client_Client(
                    "client-ios",
                    "ios"),
                metrics:
                new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_CoordinateClientUsageMetrics(
                    new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_CoordinateClientUsageOperationInsightsConnection(
                        iosOperations)));

        var metrics = new CoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_FieldCoordinateMetrics(
            new ICoordinateOperationsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages[]
            {
                webClientUsage,
                iosClientUsage
            });

        var coordinateData =
            new CoordinateOperationsQuery_ApiById_Stages_Coordinate_GraphQLObjectFieldDefinition(
                coordinate: coordinate,
                isDeprecated: false,
                metrics: metrics);

        var stage = new CoordinateOperationsQuery_ApiById_Stages_Stage(
            name: Stage,
            coordinate: coordinateData);

        CoordinatesClientMock
            .Setup(x => x.GetCoordinateOperationsAsync(
                ApiId,
                Stage,
                coordinate,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
    }
}
