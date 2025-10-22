using System.Net.Http.Headers;

namespace HotChocolate.Exporters.OpenApi;

// TODO: With authorization also check what happens if we handle it in validation
public class HttpEndpointIntegrationTests : OpenApiTestBase
{
    // TODO: Test with values of the wrong type in the URL
    // TODO: Test with query parameters

    #region GET

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
        var response = await client.GetAsync("/users");

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
        var response = await client.GetAsync("/users");

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
        var response = await client.GetAsync("/users");

        // assert
        response.MatchSnapshot();
    }

    #endregion

    // TODO: Test with values omitted or of the wrong type

    #region POST

    [Fact]
    public async Task Http_Post()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new StringContent(
            """
            {
              "id": "6",
              "name": "Test",
              "email": "Email"
            }
            """);

        var response = await client.PostAsync("/users", content);

        // assert
        response.MatchSnapshot();
    }

    #endregion
}
