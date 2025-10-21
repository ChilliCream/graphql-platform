namespace HotChocolate.Exporters.OpenApi;

public class IntegrationTests : OpenApiTestBase
{
    [Fact]
    public void Test()
    {
        // arrange
        var storage = new TestOpenApiDocumentStorage();
        var server = CreateBasicTestServer(storage);

        // act

        // assert
    }
}
