using System.Net.Http.Headers;

namespace HotChocolate.Exporters.OpenApi;

public class HttpEndpointIntegrationTests : OpenApiTestBase
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
    public async Task Http_Get_Has_GraphQL_Errors()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users/5");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_Root_Field_Has_Authorization()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenHelper.GenerateToken());

        // act
        var response = await client.GetAsync("/orders");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_Root_Field_Has_Authorization_Not_Authenticated()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/orders");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_Root_Field_Has_Authorization_Not_Authorized()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenHelper.GenerateToken("guest"));

        // act
        var response = await client.GetAsync("/orders");

        // assert
        response.MatchSnapshot();
    }
}
