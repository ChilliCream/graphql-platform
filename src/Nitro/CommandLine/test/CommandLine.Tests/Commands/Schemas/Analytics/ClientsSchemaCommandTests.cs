using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas.Analytics;

public sealed class ClientsSchemaCommandTests(NitroCommandFixture fixture)
    : AnalyticsCommandTestBase(fixture)
{
    private const string Coordinate = "User.email";

    [Fact]
    public async Task Clients_Markdown_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateClients(Coordinate);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "clients",
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

            ## Clients (3)

            | Client | Versions | Operations | Requests |
            |---|---|---|---|
            | web | 3 | 12 | 2,104,332 |
            | ios | 2 | 8 | 1,847,221 |
            | android | 2 | 3 | 267,151 |
            """);
    }

    [Fact]
    public async Task Clients_Json_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateClients(Coordinate);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "clients",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", Coordinate,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "json");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("\"coordinate\": \"User.email\"", result.StdOut);
        Assert.Contains("\"web\"", result.StdOut);
        Assert.Contains("2104332", result.StdOut);
    }

    [Fact]
    public async Task Clients_Table_ReturnsSuccess()
    {
        // arrange
        SetupCoordinateClients(Coordinate);

        // act
        var result = await ExecuteCommandAsync(
            "schema", "clients",
            "--api-id", ApiId,
            "--stage", Stage,
            "--coordinate", Coordinate,
            "--from", "2026-04-06T00:00:00Z",
            "--to", "2026-04-13T00:00:00Z",
            "--format", "table");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Contains("web", result.StdOut);
        Assert.Contains("ios", result.StdOut);
        Assert.Contains("android", result.StdOut);
        Assert.Contains("2,104,332", result.StdOut);
    }

    private void SetupCoordinateClients(string coordinate)
    {
        var clientUsages = new ICoordinateClientsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages[]
        {
            new CoordinateClientsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_CoordinateClientUsage(
                name: "web",
                totalVersions: 3L,
                totalOperations: 12L,
                totalRequests: 2_104_332L,
                client: new CoordinateClientsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Client_Client(
                    "client-web",
                    "web")),
            new CoordinateClientsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_CoordinateClientUsage(
                name: "ios",
                totalVersions: 2L,
                totalOperations: 8L,
                totalRequests: 1_847_221L,
                client: new CoordinateClientsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Client_Client(
                    "client-ios",
                    "ios")),
            new CoordinateClientsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_CoordinateClientUsage(
                name: "android",
                totalVersions: 2L,
                totalOperations: 3L,
                totalRequests: 267_151L,
                client: new CoordinateClientsQuery_ApiById_Stages_Coordinate_Metrics_ClientUsages_Client_Client(
                    "client-android",
                    "android"))
        };

        var metrics =
            new CoordinateClientsQuery_ApiById_Stages_Coordinate_Metrics_FieldCoordinateMetrics(clientUsages);

        var coordinateData =
            new CoordinateClientsQuery_ApiById_Stages_Coordinate_GraphQLObjectFieldDefinition(
                coordinate: coordinate,
                isDeprecated: false,
                metrics: metrics);

        var stage = new CoordinateClientsQuery_ApiById_Stages_Stage(
            name: Stage,
            coordinate: coordinateData);

        CoordinatesClientMock
            .Setup(x => x.GetCoordinateClientsAsync(
                ApiId,
                Stage,
                coordinate,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stage);
    }
}
