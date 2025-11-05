namespace HotChocolate.Exporters.OpenApi;

// TODO: Test with arrays, custom scalars, enum, interface, union, etc., introspection fields
// TODO: @skip and such needs to be treated as optional
// TODO: Test different serialization types, patterns and formats of scalars
public abstract class OpenApiIntegrationTestBase : OpenApiTestBase
{
    [Fact]
    public async Task OpenApi_Includes_Initial_Routes()
    {
        // arrange
        var storage = CreateBasicTestDocumentStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task OperationDocument()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                name
                email
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task OperationDocument_With_Local_Fragment()
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
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task OperationDocument_With_FragmentDocument_Reference()
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
            """,
            """
            fragment User on User {
              id
              name
              email
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task OperationDocument_With_Local_Fragment_With_FragmentDocument_Reference()
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
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task OperationDocument_With_Fields_And_Local_Fragment()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
                ...User
              }
            }

            fragment User on User {
              name
              email
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task OperationDocument_With_Fields_And_FragmentDocument_Reference()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
                ...User
              }
            }
            """,
            """
            fragment User on User {
              name
              email
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task FragmentDocument()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                name
                email
              }
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task FragmentDocument_With_Local_Fragment()
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
            """,
            """
            fragment User on User {
              id
              name
              email
              address {
                ...Address
              }
            }

            fragment Address on Address {
              street
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task FragmentDocument_With_FragmentDocument_Reference()
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
            """,
            """
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
            fragment Address on Address {
              street
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task FragmentDocument_With_Local_Fragment_With_FragmentDocument_Reference()
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
            """,
            """
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
            fragment Address on Address {
              street
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task FragmentDocument_With_Fields_And_Local_Fragment()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
                ...User
              }
            }
            """,
            """
            fragment User on User {
              id
              ...LocalUser
            }

            fragment LocalUser on User {
              name
              email
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact]
    public async Task FragmentDocument_With_Fields_And_FragmentDocument_Reference()
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
            """,
            """
            fragment User on User {
              id
              ...GlobalUser
            }
            """,
            """
            fragment GlobalUser on User {
              name
              email
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }
}
