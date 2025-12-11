using HotChocolate.Execution;

namespace HotChocolate.Adapters.OpenApi;

public abstract class ValidationTestBase : OpenApiTestBase
{
    #region Fragment Document

    [Fact]
    public async Task Fragment_Name_Not_Unique_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            fragment User on User {
              id
              name
            }
            """,
            """
            # This fragment name is intentionally duplicate
            fragment User on User {
              id
              email
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Fragment name 'User' is already being used by a fragment document with the Id '0' ('User').",
            error.Message);
    }

    [Fact]
    public async Task Fragment_Reference_Does_Not_Exist_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            fragment User on User {
              id
              name
              ...NonExistentFragment
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Fragment 'NonExistentFragment' referenced by fragment document 'User' does not exist.",
            error.Message);
    }

    [Fact]
    public async Task Fragment_Invalid_TypeCondition_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            fragment User on NonExistentType {
              id
              name
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Type condition 'NonExistentType' not found in schema.", error.Message);
    }

    [Fact]
    public async Task Fragment_Contains_Defer_Directive_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            fragment User on User {
              id
              ... @defer {
                name
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Fragment document 'User' contains the '@defer' directive, which is not allowed in OpenAPI documents.", error.Message);
    }

    [Fact]
    public async Task Fragment_Contains_Stream_Directive_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            fragment Query on Query {
              users @stream {
                name
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Fragment document 'Query' contains the '@stream' directive, which is not allowed in OpenAPI documents.", error.Message);
    }

    [Fact]
    public async Task Fragment_Removal_When_Referenced_By_Operation_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            fragment User on User {
              id
              name
            }
            """,
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                ...User
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        storage.RemoveDocument("0");

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Cannot remove fragment 'User' because it is still referenced by the following operations: 'GetUser'.",
            error.Message);
    }

    #endregion

    #region Operation Document

    [Fact]
    public async Task Operation_Name_Not_Unique_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
              }
            }
            """,
            """
            # This operation name is intentionally duplicate
            query getUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal(
            "Operation name 'getUserById' is already being used by a operation document with the Id '0' ('GetUserById').",
            error.Message);
    }

    [Fact]
    public async Task Operation_Fragment_Reference_Does_Not_Exist_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                ...NonExistentFragment
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Fragment 'NonExistentFragment' referenced by operation document 'GetUser' does not exist.",
            error.Message);
    }

    [Fact]
    public async Task Operation_Is_Subscription_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            subscription OnUserUpdated($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userUpdated(id: $userId) {
                id
                name
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal(
            "Operation 'OnUserUpdated' is a subscription. Only queries and mutations are allowed for OpenAPI operations.",
            error.Message);
    }

    [Fact]
    public async Task Operation_Multiple_Root_Fields_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUsers @http(method: GET, route: "/users") {
              users {
                id
              }
              userById(id: "1") {
                name
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Operation 'GetUsers' must have exactly one root field selection, but found 2.", error.Message);
    }

    [Fact]
    public async Task Operation_Root_Fragment_Spread_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            fragment User on User {
              id
              name
            }
            """,
            """
            query GetUser @http(method: GET, route: "/user") {
              ...User
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal(
            "Operation 'GetUser' must have a single root field selection, but found a fragment spread or inline fragment.",
            error.Message);
    }

    [Fact]
    public async Task Operation_Parameter_Conflict_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}", queryParameters: ["userId"]) {
              userById(id: $userId) {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Operation 'GetUser' has conflicting parameters that map to '$userId'.", error.Message);
    }

    [Fact]
    public async Task Operation_Parameter_Conflict_Same_Location_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            mutation UpdateUser($user: UserInput! @body) @http(method: PUT, route: "/users/{userId:$user.id}", queryParameters: ["id:$user.id"]) {
              updateUser(user: $user) {
                id
                name
                email
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Operation 'UpdateUser' has conflicting parameters that map to '$user.id'.", error.Message);
    }

    [Fact]
    public async Task Operation_Does_Not_Compile_Against_Schema_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              nonExistentField(id: $userId) {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal(
            "Operation 'GetUser' does not compile against the schema: The field `nonExistentField` does not exist on the type `Query`.",
            error.Message);
    }

    [Fact]
    public async Task Operation_Missing_Name_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query @http(method: GET, route: "/users") {
              users {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Operation must have a name.", error.Message);
    }

    [Fact]
    public async Task Operation_Missing_HttpDirective_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUsers {
              users {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Operation 'GetUsers' must be annotated with @http directive.", error.Message);
    }

    [Fact]
    public async Task Operation_Missing_Method_Argument_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUsers @http(route: "/users") {
              users {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("@http directive for operation 'GetUsers' must have a 'method' argument.", error.Message);
    }

    [Fact]
    public async Task Operation_Missing_Route_Argument_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUsers @http(method: GET) {
              users {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("@http directive for operation 'GetUsers' must have a 'route' argument.", error.Message);
    }

    [Fact]
    public async Task Operation_Invalid_HttpMethod_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUsers @http(method: INVALID, route: "/users") {
              users {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Invalid HTTP method value 'INVALID' on @http directive for operation 'GetUsers'.", error.Message);
    }

    [Fact]
    public async Task Operation_Empty_Route_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUsers @http(method: GET, route: "") {
              users {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Route argument on @http directive for operation 'GetUsers' must be a non-empty string.",
            error.Message);
    }

    [Fact]
    public async Task Operation_Invalid_Route_Parameter_Syntax_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUser @http(method: GET, route: "/users/{userId:invalid}") {
              userById(id: $userId) {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal(
            "Explicit route segment variable mappings must start with '$', got 'userId:invalid' in operation 'GetUser'.",
            error.Message);
    }

    [Fact]
    public async Task Operation_Invalid_QueryParameters_Type_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUsers @http(method: GET, route: "/users", queryParameters: 123) {
              users {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Query parameters argument on @http directive for operation 'GetUsers' must be a list of strings.",
            error.Message);
    }

    [Fact]
    public async Task Operation_Invalid_QueryParameters_Item_Type_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUsers @http(method: GET, route: "/users", queryParameters: [123, "valid"]) {
              users {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal(
            "Query parameters argument on @http directive for operation 'GetUsers' must contain only string values.",
            error.Message);
    }

    [Fact]
    public async Task Document_Multiple_Operations_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUsers @http(method: GET, route: "/users") {
              users {
                id
              }
            }
            query GetUser @http(method: GET, route: "/user") {
              user {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("An operation document can only define a single operation alongside local fragment definitions.",
            error.Message);
    }

    [Fact]
    public async Task Document_No_Operation_Or_Fragment_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            type Query {
              users: [User]
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Document must contain either a single operation or at least one fragment definition.",
            error.Message);
    }

    [Fact]
    public async Task Operation_Route_Pattern_Duplicate_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
              }
            }
            """,
            """
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Route pattern '/users/{userId}' with HTTP method 'GET' is already being used by operation document 'GetUser' with Id '0'.", error.Message);
    }

    [Fact]
    public async Task Operation_Route_Pattern_Duplicate_CaseInsensitive_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/Users/{userId}") {
              userById(id: $userId) {
                id
              }
            }
            """,
            """
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Route pattern '/users/{userId}' with HTTP method 'GET' is already being used by operation document 'GetUser' with Id '0'.", error.Message);
    }

    [Fact]
    public async Task Operation_Route_Pattern_Same_With_Different_Method_Does_Not_RaiseError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
              }
            }
            """,
            """
            mutation UpdateUser($user: UserInput! @body) @http(method: PUT, route: "/users/{userId:$user.id}") {
              updateUser(user: $user) {
                id
                name
                email
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        // assert
        Assert.Empty(eventListener.Errors);
    }

    [Fact]
    public async Task Operation_Contains_Defer_Directive_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) @defer {
                id
                ... @defer {
                  name
                }
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Operation document 'GetUser' contains the '@defer' directive, which is not allowed in OpenAPI documents.", error.Message);
    }

    [Fact]
    public async Task Operation_Contains_Stream_Directive_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDocumentStorage(
            """
            query GetUsers @http(method: GET, route: "/users") {
              users @stream {
                name
              }
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Operation document 'GetUsers' contains the '@stream' directive, which is not allowed in OpenAPI documents.", error.Message);
    }

    #endregion
}
