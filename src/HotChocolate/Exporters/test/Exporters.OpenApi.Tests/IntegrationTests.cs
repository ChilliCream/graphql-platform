namespace HotChocolate.Exporters.OpenApi;

public class IntegrationTests : OpenApiTestBase
{
    [Fact]
    public async Task Execute_Get()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users/15");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task OpenApi_Includes_Initial_Routes()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(extension: ".json");
    }
}
