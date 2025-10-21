namespace HotChocolate.Exporters.OpenApi;

public class IntegrationTests : OpenApiTestBase
{
    [Fact]
    public async Task Http_Get()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users/1");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_Root_Field_Returns_Null_Without_Errors()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users/4");

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
