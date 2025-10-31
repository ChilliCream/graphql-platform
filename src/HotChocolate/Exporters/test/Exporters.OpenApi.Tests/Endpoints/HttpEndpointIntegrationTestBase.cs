using System.Net.Http.Headers;
using System.Text;

namespace HotChocolate.Exporters.OpenApi;

// TODO: We need to validate that we can't have the same path + method twice, even if once with and without query parameters
// TODO: With authorization also check what happens if we handle it in validation
// TODO: Test hot reload
// TODO: @oneOf tests
public abstract class HttpEndpointIntegrationTestBase : OpenApiTestBase
{
    #region GET

    [Fact]
    public async Task Http_Get()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users/1");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_With_Query_Parameter()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users/1/details?includeAddress=true");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_Invalid_Type_In_Route_Parameter()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users/abc");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_Root_Field_Returns_Null_Without_Errors()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
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
        var server = CreateTestServer(storage);
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
        var server = CreateTestServer(storage);
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
        var server = CreateTestServer(storage);
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
        var server = CreateTestServer(storage);
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

    #region POST

    [Fact]
    public async Task Http_Post()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new StringContent(
            """
            {
              "id": "6",
              "name": "Test",
              "email": "Email"
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/users", content);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Post_Invalid_ContentType()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = "6", ["name"] = "Test", ["email"] = "Email"
        });

        var response = await client.PostAsync("/users", content);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Post_Body_Field_Has_Wrong_Type()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new StringContent(
            """
            {
              "id": "6",
              "name": "Test",
              "email": 123
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/users", content);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Post_Body_Missing_Field()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new StringContent(
            """
            {
              "id": "6",
              "name": "Test"
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/users", content);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Post_Without_Body()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        var content = new StringContent("", Encoding.UTF8, "application/json");

        // act
        var response = await client.PostAsync("/users", content);

        // assert
        response.MatchSnapshot();
    }

    #endregion

    #region PUT

    [Fact]
    public async Task Http_Put()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new StringContent(
            """
            {
              "name": "Test",
              "email": "Email"
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PutAsync("/users/6", content);

        // assert
        response.MatchSnapshot();
    }

    #endregion
}
