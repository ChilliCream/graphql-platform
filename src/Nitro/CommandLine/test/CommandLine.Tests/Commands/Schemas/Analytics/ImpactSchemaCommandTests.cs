using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas.Analytics;

public sealed class ImpactSchemaCommandTests(NitroCommandFixture fixture)
    : AnalyticsCommandTestBase(fixture)
{
    private const string Coordinate = "User.email";

    [Fact]
    public async Task Impact_Markdown_ReturnsSuccess_WhenCoordinateInUse()
    {
        // arrange
        SetupCoordinateImpact(Coordinate, isDeprecated: false);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "impact",
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
            verdict: UNSAFE_TO_REMOVE
            ---

            ## Usage

            | Metric | Value |
            |---|---|
            | Total requests | 4,218,704 |
            | Clients | 2 |
            | Operations | 2 |
            | First seen | 2024-11-02 |
            | Last seen | 2026-04-13 |
            | Error rate | 0.03% |

            ## Clients (2)

            | Client | Versions | Operations | Requests |
            |---|---|---|---|
            | web | 3 | 12 | 2,104,332 |
            | ios | 2 | 8 | 1,847,221 |

            ## Operations (2)

            | Operation | Kind | Client | Requests | Error rate |
            |---|---|---|---|---|
            | GetCurrentUser | QUERY | web | 1,203,441 | 0% |
            | LoginFlow | QUERY | ios | 894,002 | 0.01% |
            """);
    }

    [Fact]
    public async Task Impact_Json_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateImpact(Coordinate, isDeprecated: false);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "impact",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", Coordinate,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "json");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("\"verdict\": \"UNSAFE_TO_REMOVE\"", result.StdOut);
        Assert.Contains("\"coordinate\": \"User.email\"", result.StdOut);
    }

    [Fact]
    public async Task Impact_Table_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateImpact(Coordinate, isDeprecated: false);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "impact",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", Coordinate,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "table");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("UNSAFE_TO_REMOVE", result.StdOut);
        Assert.Contains("web", result.StdOut);
        Assert.Contains("GetCurrentUser", result.StdOut);
    }

    [Fact]
    public async Task Impact_Verdict_IsReadyToRemove_WhenDeprecatedAndZeroUsage()
    {
        // arrange
        SetupCoordinateImpactEmpty("Review.legacyScore", isDeprecated: true);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "impact",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", "Review.legacyScore",
            "--format", "json");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("\"verdict\": \"READY_TO_REMOVE\"", result.StdOut);
    }

    [Fact]
    public async Task Impact_Verdict_IsSafeToRemove_WhenNotDeprecatedAndZeroUsage()
    {
        // arrange
        SetupCoordinateImpactEmpty("Review.unused", isDeprecated: false);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "impact",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", "Review.unused",
            "--format", "json");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("\"verdict\": \"SAFE_TO_REMOVE\"", result.StdOut);
    }

    private void SetupCoordinateImpact(string coordinate, bool isDeprecated)
    {
        var usage = new CoordinateImpactQuery_ApiById_Stages_Coordinate_Usage_CoordinateUsage(
            totalRequests: 4_218_704L,
            clientCount: 2L,
            operationCount: 2L,
            firstSeen: new DateTimeOffset(2024, 11, 2, 0, 0, 0, TimeSpan.Zero),
            lastSeen: new DateTimeOffset(2026, 4, 13, 0, 0, 0, TimeSpan.Zero),
            errorRate: 0.0003,
            meanDuration: 42.1);

        var webOperations =
            new ICoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_Nodes[]
            {
                new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_Nodes_CoordinateClientUsageOperationInsight(
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
            new ICoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_Nodes[]
            {
                new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_Nodes_CoordinateClientUsageOperationInsight(
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

        var webUsage =
            new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_CoordinateClientUsage(
                name: "web",
                totalVersions: 3L,
                totalOperations: 12L,
                totalRequests: 2_104_332L,
                client: new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Client_Client(
                    "client-web", "web"),
                metrics:
                new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_CoordinateClientUsageMetrics(
                    new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_CoordinateClientUsageOperationInsightsConnection(
                        webOperations)));

        var iosUsage =
            new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_CoordinateClientUsage(
                name: "ios",
                totalVersions: 2L,
                totalOperations: 8L,
                totalRequests: 1_847_221L,
                client: new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Client_Client(
                    "client-ios", "ios"),
                metrics:
                new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_CoordinateClientUsageMetrics(
                    new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Metrics_Operations_CoordinateClientUsageOperationInsightsConnection(
                        iosOperations)));

        var metrics = new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_FieldCoordinateMetrics(
            new ICoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages[]
            {
                webUsage,
                iosUsage
            });

        var coordinateData =
            new CoordinateImpactQuery_ApiById_Stages_Coordinate_GraphQLObjectFieldDefinition(
                coordinate: coordinate,
                isDeprecated: isDeprecated,
                usage: usage,
                metrics: metrics);

        var stage = new CoordinateImpactQuery_ApiById_Stages_Stage(
            name: Stage,
            coordinate: coordinateData);

        CoordinatesClientMock
            .Setup(x => x.GetCoordinateImpactAsync(
                ApiId,
                Stage,
                coordinate,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
    }

    private void SetupCoordinateImpactEmpty(string coordinate, bool isDeprecated)
    {
        var usage = new CoordinateImpactQuery_ApiById_Stages_Coordinate_Usage_CoordinateUsage(
            totalRequests: 0L,
            clientCount: 0L,
            operationCount: 0L,
            firstSeen: null,
            lastSeen: null,
            errorRate: null,
            meanDuration: null);

        var metrics = new CoordinateImpactQuery_ApiById_Stages_Coordinate_Metrics_FieldCoordinateMetrics(
            []);

        var coordinateData =
            new CoordinateImpactQuery_ApiById_Stages_Coordinate_GraphQLObjectFieldDefinition(
                coordinate: coordinate,
                isDeprecated: isDeprecated,
                usage: usage,
                metrics: metrics);

        var stage = new CoordinateImpactQuery_ApiById_Stages_Stage(
            name: Stage,
            coordinate: coordinateData);

        CoordinatesClientMock
            .Setup(x => x.GetCoordinateImpactAsync(
                ApiId,
                Stage,
                coordinate,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
    }
}
