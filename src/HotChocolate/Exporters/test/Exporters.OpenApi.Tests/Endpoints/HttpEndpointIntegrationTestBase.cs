using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using HotChocolate;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

// TODO: We need to validate that we can't have the same path + method twice, even if once with and without query parameters
// TODO: With authorization also check what happens if we handle it in validation
// TODO: @oneOf tests
// TODO: Test with a long value in either route or query
// TODO: Test result with timeout
// TODO: Test schema hot reload
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
    public async Task Http_Get_With_Query_Parameter_Boolean_Value_For_Boolean()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUser($includeEmail: Boolean!) @http(method: GET, route: "/users", queryParameters: ["includeEmail"]) {
              users {
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
            query GetUser($userId: ID!) @http(method: GET, route: "/users-details", queryParameters: ["userId"]) {
              userById(id: $userId) {
                id
                name
                email
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var response = await client.GetAsync("/users-details?userId=true");

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

    #region Hot Reload

    [Fact]
    public async Task HotReload_Add_New_Document()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();
        var registry = server.Services.GetRequiredKeyedService<OpenApiDocumentRegistry>(ISchemaDefinition.DefaultName);
        var documentUpdatedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using var subscription = registry.Subscribe(new OpenApiDocumentEventObserver(@event =>
        {
            if (@event.Type == OpenApiDocumentEventType.Updated)
            {
                documentUpdatedResetEvent.Set();
            }
        }));

        // act
        // assert
        var response1 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.NotFound, response1.StatusCode);

        await storage.AddOrUpdateDocumentAsync(
            "new",
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
              }
            }
            """);

        documentUpdatedResetEvent.Wait(cts.Token);

        var response2 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        response2.MatchSnapshot();
    }

    [Fact]
    public async Task HotReload_Update_Existing_Document()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        await storage.AddOrUpdateDocumentAsync(
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
        var registry = server.Services.GetRequiredKeyedService<OpenApiDocumentRegistry>(ISchemaDefinition.DefaultName);
        var documentUpdatedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using var subscription = registry.Subscribe(new OpenApiDocumentEventObserver(@event =>
        {
            if (@event.Type == OpenApiDocumentEventType.Updated)
            {
                documentUpdatedResetEvent.Set();
            }
        }));

        // act
        // assert
        var response1 = await client.GetAsync("/users");
        var content1 = await response1.Content.ReadAsStringAsync();

        await storage.AddOrUpdateDocumentAsync(
            "users",
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
                name
              }
            }
            """);

        documentUpdatedResetEvent.Wait(cts.Token);

        var response2 = await client.GetAsync("/users");
        var content2 = await response2.Content.ReadAsStringAsync();

        Assert.NotEqual(content2, content1);

        response2.MatchSnapshot();
    }

    [Fact]
    public async Task HotReload_Update_Existing_Document_Different_Route()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        await storage.AddOrUpdateDocumentAsync(
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
        var registry = server.Services.GetRequiredKeyedService<OpenApiDocumentRegistry>(ISchemaDefinition.DefaultName);
        var documentUpdatedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using var subscription = registry.Subscribe(new OpenApiDocumentEventObserver(@event =>
        {
            if (@event.Type == OpenApiDocumentEventType.Updated)
            {
                documentUpdatedResetEvent.Set();
            }
        }));

        // act
        // assert
        var oldRouteResponse1 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, oldRouteResponse1.StatusCode);

        await storage.AddOrUpdateDocumentAsync(
            "users",
            """
            query GetUsers @http(method: GET, route: "/users-new") {
              usersWithoutAuth {
                id
                name
              }
            }
            """);

        documentUpdatedResetEvent.Wait(cts.Token);

        var newRouteResponse = await client.GetAsync("/users-new");

        Assert.Equal(HttpStatusCode.OK, newRouteResponse.StatusCode);

        var oldRouteResponse2 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.NotFound, oldRouteResponse2.StatusCode);

        newRouteResponse.MatchSnapshot();
    }

    [Fact(Skip = "Need to determine what best behavior should be")]
    public async Task HotReload_Update_Existing_Document_With_Invalid_Document()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        await storage.AddOrUpdateDocumentAsync(
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

        await storage.AddOrUpdateDocumentAsync(
            "users",
            // This is intentionally missing the @http directive to be invalid
            """
            query GetUsers {
              usersWithoutAuth {
                id
              }
            }
            """);

        // TODO: Add assertion for after the failed update
    }

    [Fact]
    public async Task HotReload_Remove_Existing_Document()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        await storage.AddOrUpdateDocumentAsync(
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
        var registry = server.Services.GetRequiredKeyedService<OpenApiDocumentRegistry>(ISchemaDefinition.DefaultName);
        var documentUpdatedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using var subscription = registry.Subscribe(new OpenApiDocumentEventObserver(@event =>
        {
            if (@event.Type == OpenApiDocumentEventType.Updated)
            {
                documentUpdatedResetEvent.Set();
            }
        }));

        // act
        // assert
        var response1 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        await storage.RemoveDocumentAsync("users");

        documentUpdatedResetEvent.Wait(cts.Token);

        var response2 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
    }

    [Fact]
    public async Task HotReload_Remove_Non_Existent_Document()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        await storage.AddOrUpdateDocumentAsync(
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

        await storage.RemoveDocumentAsync("non-existent-id");

        var response2 = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    #endregion
}
