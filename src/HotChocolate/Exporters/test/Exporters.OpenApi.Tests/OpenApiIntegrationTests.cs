namespace HotChocolate.Exporters.OpenApi;

public class OpenApiIntegrationTests : OpenApiTestBase
{
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
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }
}
