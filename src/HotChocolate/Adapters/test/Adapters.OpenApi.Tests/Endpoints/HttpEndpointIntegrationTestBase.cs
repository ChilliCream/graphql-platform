using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace HotChocolate.Adapters.OpenApi;

public abstract class HttpEndpointIntegrationTestBase : OpenApiTestBase
{
    #region GET

    [Fact]
    public async Task Http_Get()
    {
        // arrange
        var storage = CreateBasicTestDefinitionStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users/1");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_With_Fragment_Referencing_Fragment()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                ...User
                ...LocalUser
              }
            }

            fragment LocalUser on User {
              email
            }
            """,
            """
            fragment User on User {
              ...LocalUser2
              name
              address {
                ...Address
              }
            }

            fragment LocalUser2 on User {
              id
            }
            """,
            """
            fragment Address on Address {
              street
            }
            """);
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
        var storage = CreateBasicTestDefinitionStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users/1/details?includeAddress=true");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_Without_Query_Parameter_That_Has_Default_Value()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetFullUser($userId: ID!, $includeAddress: Boolean! = true)
              @http(method: GET, route: "/users/{userId}/details", queryParameters: ["includeAddress"]) {
              userById(id: $userId) {
                id
                name
                address @include(if: $includeAddress) {
                  street
                }
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users/1/details");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_With_Query_Parameter_Boolean_Value_For_Boolean()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUsers($includeEmail: Boolean!) @http(method: GET, route: "/users", queryParameters: ["includeEmail"]) {
              usersWithoutAuth {
                id
                name
                email @include(if: $includeEmail)
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users?includeEmail=true");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_With_Query_Parameter_Boolean_Value_For_String()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUser($userName: String!) @http(method: GET, route: "/users-details", queryParameters: ["userName"]) {
              userByName(name: $userName) {
                id
                name
                email
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users-details?userName=true");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Get_Invalid_Type_In_Route_Parameter()
    {
        // arrange
        var storage = CreateBasicTestDefinitionStorage();
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
        var storage = CreateBasicTestDefinitionStorage();
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
        var storage = CreateBasicTestDefinitionStorage();
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
        var storage = CreateBasicTestDefinitionStorage();
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
        var storage = CreateBasicTestDefinitionStorage();
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
        var storage = CreateBasicTestDefinitionStorage();
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
        var storage = CreateBasicTestDefinitionStorage();
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
    public async Task Http_Post_Complex_Object()
    {
        // arrange
        var storage = CreateBasicTestDefinitionStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new StringContent(
            """
            {
                "any": {
                  "key": "value"
                },
                "boolean": true,
                "byte": 1,
                "byteArray": "dGVzdA==",
                "date": "2000-01-01",
                "dateTime": "2000-01-01T12:00:00.000Z",
                "decimal": 79228162514264337593543950335,
                "enum": "VALUE1",
                "float": 1.5,
                "id": "test",
                "int": 1,
                "json": {
                  "key": "value"
                },
                "list": [
                  "test"
                ],
                "localDate": "2000-01-01",
                "localDateTime": "2000-01-01T12:00:00",
                "localTime": "12:00:00",
                "long": 9223372036854775807,
                "object": {
                  "field1A": {
                    "field1B": {
                      "field1C": "12:00:00"
                    }
                  }
                },
                "short": 1,
                "string": "test",
                "timeSpan": "PT5M",
                "unknown": "test",
                "url": "https://example.com/",
                "uuid": "00000000-0000-0000-0000-000000000000"
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/complex", content);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Post_Empty_List()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query TestQuery($input: [String!]! @body) @http(method: POST, route: "/example") {
              list(input: $input)
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new StringContent(
            """
            []
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/example", content);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Post_Empty_Object()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query TestQuery($input: Any! @body) @http(method: POST, route: "/example") {
              json(input: $input)
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new StringContent(
            """
            {}
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/example", content);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Post_Invalid_ContentType()
    {
        // arrange
        var storage = CreateBasicTestDefinitionStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = "6",
            ["name"] = "Test",
            ["email"] = "Email"
        });

        var response = await client.PostAsync("/users", content);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Http_Post_Body_Missing_Field()
    {
        // arrange
        var storage = CreateBasicTestDefinitionStorage();
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
        var storage = CreateBasicTestDefinitionStorage();
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
        var storage = CreateBasicTestDefinitionStorage();
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

    [Fact]
    public async Task Http_Put_Deeply_Nested_Input_Without_Query_Parameter_That_Has_Default_Value()
    {
        // arrange
        var storage = CreateBasicTestDefinitionStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var content = new StringContent(
            """
            {
              "field": "Test",
              "object": {
                "otherField": "Test2"
              }
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PutAsync("/object/6", content);

        // assert
        response.MatchSnapshot();
    }

    #endregion

    #region Hot Reload

    [Fact]
    public async Task HotReload_Add_New_Document()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        // assert
        var response1 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.NotFound, response1.StatusCode);

        storage.AddOrUpdateDocument(
            "new",
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
              }
            }
            """);

        HttpResponseMessage? response2 = null;
        await SpinWaitAsync(async () =>
        {
            response2 = await client.GetAsync("/users");
            return response2.StatusCode == HttpStatusCode.OK;
        }, cts.Token);

        response2!.MatchSnapshot();
    }

    [Fact]
    public async Task HotReload_Update_Existing_Document()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        storage.AddOrUpdateDocument(
            "users",
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        // assert
        var response1 = await client.GetAsync("/users");
        var content1 = await response1.Content.ReadAsStringAsync();

        storage.AddOrUpdateDocument(
            "users",
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
                name
              }
            }
            """);

        HttpResponseMessage? response2 = null;
        await SpinWaitAsync(async () =>
        {
            response2 = await client.GetAsync("/users");
            var content = await response2.Content.ReadAsStringAsync();
            return content != content1;
        }, cts.Token);

        response2!.MatchSnapshot();
    }

    [Fact]
    public async Task HotReload_Update_Existing_Document_Different_Route()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        storage.AddOrUpdateDocument(
            "users",
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        // assert
        var oldRouteResponse1 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, oldRouteResponse1.StatusCode);

        storage.AddOrUpdateDocument(
            "users",
            """
            query GetUsers @http(method: GET, route: "/users-new") {
              usersWithoutAuth {
                id
                name
              }
            }
            """);

        HttpResponseMessage? newRouteResponse = null;
        await SpinWaitAsync(async () =>
        {
            newRouteResponse = await client.GetAsync("/users-new");
            return newRouteResponse.StatusCode == HttpStatusCode.OK;
        }, cts.Token);

        var oldRouteResponse2 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.NotFound, oldRouteResponse2.StatusCode);

        newRouteResponse!.MatchSnapshot();
    }

    [Fact]
    public async Task HotReload_Remove_Existing_Operation()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        storage.AddOrUpdateDocument(
            "users",
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        // assert
        var response1 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        storage.RemoveDocument("users");

        await SpinWaitAsync(async () =>
        {
            var response = await client.GetAsync("/users");
            return response.StatusCode == HttpStatusCode.NotFound;
        }, cts.Token);
    }

    [Fact]
    public async Task HotReload_Remove_Non_Existent_Document()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        storage.AddOrUpdateDocument(
            "users",
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        // assert
        var response1 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        storage.RemoveDocument("non-existent-id");

        var response2 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    #endregion

    #region Invalid

    [Fact]
    public async Task Missing_Field()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              nonExistentField(id: $userId) {
                id
              }
            }
            """,
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        // assert
        var validResponse = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);

        var invalidResponse = await client.GetAsync("/users/1");

        Assert.Equal(HttpStatusCode.InternalServerError, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task Missing_Model_References()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              nonExistentField(id: $userId) {
                ...User
              }
            }
            """,
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        // assert
        var validResponse = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);

        var invalidResponse = await client.GetAsync("/users/1");

        Assert.Equal(HttpStatusCode.InternalServerError, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task Duplicated_Routes()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUsersWithName @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
                name
              }
            }
            """,
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                address {
                  street
                }
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Duplicated_Model_Names()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                ...User
              }
            }
            """,
            """
            fragment User on User {
              address {
                street
              }
            }
            """,
            """
            fragment User on User {
              id
              name
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users");

        // assert
        response.MatchSnapshot();
    }

    #endregion
}
