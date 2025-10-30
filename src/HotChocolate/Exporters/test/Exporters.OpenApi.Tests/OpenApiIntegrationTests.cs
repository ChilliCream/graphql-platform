namespace HotChocolate.Exporters.OpenApi;

// TODO: Test with arrays, custom scalars, enum, interface, union, etc.
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

    [Fact]
    public async Task Local_Fragment()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                ...User
              }
            }

            fragment User on User {
              id
              name
              email
            }
            """);
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task Local_Fragment_References_Fragment_Document()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                ...User
              }
            }

            fragment User on User {
              id
              name
              email
              address {
                ...Address
              }
            }
            """,
            """
            "An address"
            fragment Address on Address {
              street
            }
            """);
        var server = CreateBasicTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }
}
