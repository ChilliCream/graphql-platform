using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public abstract class OpenApiIntegrationTestBase : OpenApiTestBase
{
    protected virtual string? SnapshotSuffix => null;

    [Fact]
    public async Task OpenApi_Includes_Initial_Routes()
    {
        // arrange
        var storage = CreateBasicTestDefinitionStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        var postFix = TestEnvironment.TargetFramework;

        if (!string.IsNullOrEmpty(SnapshotSuffix))
        {
            postFix = $"{postFix}_{SnapshotSuffix}";
        }

        // assert
        openApiDocument.MatchSnapshot(postFix: postFix, extension: ".json");
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
    public async Task OperationDocument_With_Default_Value_For_Variable()
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
    public async Task OperationDocument_With_FragmentDocument_Reference_On_Nullable_Field()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                preferences {
                  ...Preferences
                }
              }
            }
            """,
            """
            fragment Preferences on Preferences {
              color
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

    [Fact]
    public async Task OperationDocument_With_List_Of_Unions()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a list of pets"
            query GetPets @http(method: GET, route: "/pets") {
              withUnionTypeList {
                petType: __typename
                ... on Cat {
                  name
                }
                ... on Cat {
                  isPurring
                }
                ... on Dog {
                  name
                  isBarking
                }
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
    public async Task OperationDocument_With_List_With_Nullable_And_NonNull_Items()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage(
            """
            "Fetches a list with nullable items"
            query GetListNullableItems @http(method: GET, route: "/list-nullable") {
              listWithNullableItems
            }
            """,
            """
            "Fetches a list with non-null items"
            query GetListNonNullItems @http(method: GET, route: "/list-nonnull") {
              listWithNonNullItems
            }
            """);
        var server = CreateTestServer(storage);
        var client = server.CreateClient();

        // act
        var openApiDocument = await GetOpenApiDocumentAsync(client);

        // assert
        openApiDocument.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    #region Hot Reload

    [Fact]
    public async Task HotReload_Add_New_Document()
    {
        // arrange
        var storage = new TestOpenApiDefinitionStorage();
        var server = CreateTestServer(storage);
        var client = server.CreateClient();
        var registry = server.Services.GetRequiredKeyedService<OpenApiDefinitionRegistry>(ISchemaDefinition.DefaultName);
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
        var openApiDocument1 = await GetOpenApiDocumentAsync(client);

        storage.AddOrUpdateDocument(
            "new",
            """
            query GetUsers @http(method: GET, route: "/users") {
              usersWithoutAuth {
                id
              }
            }
            """);

        documentUpdatedResetEvent.Wait(cts.Token);

        var openApiDocument2 = await GetOpenApiDocumentAsync(client);

        Assert.NotEqual(openApiDocument1, openApiDocument2);
        openApiDocument2.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
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
        var registry = server.Services.GetRequiredKeyedService<OpenApiDefinitionRegistry>(ISchemaDefinition.DefaultName);
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
        var openApiDocument1 = await GetOpenApiDocumentAsync(client);

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

        documentUpdatedResetEvent.Wait(cts.Token);

        var openApiDocument2 = await GetOpenApiDocumentAsync(client);

        Assert.NotEqual(openApiDocument2, openApiDocument1);
        openApiDocument2.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
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
        var registry = server.Services.GetRequiredKeyedService<OpenApiDefinitionRegistry>(ISchemaDefinition.DefaultName);
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
        var openApiDocument1 = await GetOpenApiDocumentAsync(client);

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

        documentUpdatedResetEvent.Wait(cts.Token);

        var openApiDocument2 = await GetOpenApiDocumentAsync(client);

        Assert.NotEqual(openApiDocument2, openApiDocument1);
        openApiDocument2.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
    }

    [Fact(Skip = "Need to determine what best behavior should be")]
    public async Task HotReload_Update_Existing_Document_With_Invalid_Document()
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
        var openApiDocument1 = await GetOpenApiDocumentAsync(client);

        storage.AddOrUpdateDocument(
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
        var registry = server.Services.GetRequiredKeyedService<OpenApiDefinitionRegistry>(ISchemaDefinition.DefaultName);
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
        var openApiDocument1 = await GetOpenApiDocumentAsync(client);

        storage.RemoveDocument("users");

        documentUpdatedResetEvent.Wait(cts.Token);

        var openApiDocument2 = await GetOpenApiDocumentAsync(client);

        Assert.NotEqual(openApiDocument2, openApiDocument1);
        openApiDocument2.MatchSnapshot(postFix: TestEnvironment.TargetFramework, extension: ".json");
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
        var openApiDocument1 = await GetOpenApiDocumentAsync(client);

        storage.RemoveDocument("non-existent-id");

        // No event should be raised, so the document should remain the same
        var openApiDocument2 = await GetOpenApiDocumentAsync(client);

        Assert.Equal(openApiDocument2, openApiDocument1);
    }

    #endregion
}
