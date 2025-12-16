using HotChocolate.Execution;

namespace HotChocolate.Adapters.OpenApi;

public abstract class ValidationTestBase : OpenApiTestBase
{
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromSeconds(5);

    #region Model

    [Fact]
    public async Task Model_Name_Not_Unique_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("Model name 'User' is already being used by another model definition ('User').",
            error.Message);
    }

    [Fact]
    public async Task Model_Reference_Does_Not_Exist_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
            """
            fragment User on User {
              id
              name
              ...NonExistentModel
            }
            """);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal("Model 'NonExistentModel' referenced by model 'User' does not exist.",
            error.Message);
    }

    [Fact]
    public async Task Model_Invalid_TypeCondition_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
    public async Task Model_Contains_Defer_Directive_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("Model 'User' contains the '@defer' directive, which is not allowed in OpenAPI definitions.", error.Message);
    }

    [Fact]
    public async Task Model_Contains_Stream_Directive_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("Model 'Query' contains the '@stream' directive, which is not allowed in OpenAPI definitions.", error.Message);
    }

    #endregion

    #region Endpoint

    [Fact]
    public async Task Endpoint_Model_Reference_Does_Not_Exist_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                ...NonExistentModel
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
        Assert.Equal("Model 'NonExistentModel' referenced by endpoint 'GetUser' does not exist.",
            error.Message);
    }

    [Fact]
    public async Task Endpoint_Is_Subscription_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
            "Endpoint 'OnUserUpdated' is a subscription. Only queries and mutations are allowed for OpenAPI endpoints.",
            error.Message);
    }

    [Fact]
    public async Task Endpoint_Multiple_Root_Fields_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("Endpoint 'GetUsers' must have exactly one root field selection, but found 2.", error.Message);
    }

    [Fact]
    public async Task Endpoint_Root_Model_Spread_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
            "Endpoint 'GetUser' must have a single root field selection, but found a fragment spread or inline fragment.",
            error.Message);
    }

    [Fact]
    public async Task Endpoint_Parameter_Conflict_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("Endpoint 'GetUser' has conflicting parameters that map to '$userId'.", error.Message);
    }

    [Fact]
    public async Task Endpoint_Parameter_Conflict_Same_Location_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("Endpoint 'UpdateUser' has conflicting parameters that map to '$user.id'.", error.Message);
    }

    [Fact]
    public async Task Endpoint_Does_Not_Compile_Against_Schema_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
            "Endpoint 'GetUser' does not compile against the schema: The field `nonExistentField` does not exist on the type `Query`.",
            error.Message);
    }

    [Fact]
    public async Task Endpoint_Route_Pattern_Duplicate_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("Route pattern '/users/{userId}' with HTTP method 'GET' is already being used by endpoint 'GetUser'.", error.Message);
    }

    [Fact]
    public async Task Endpoint_Route_Pattern_Duplicate_CaseInsensitive_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("Route pattern '/users/{userId}' with HTTP method 'GET' is already being used by endpoint 'GetUser'.", error.Message);
    }

    [Fact]
    public async Task Endpoint_Route_Pattern_Same_With_Different_Method_Does_Not_RaiseError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
    public async Task Endpoint_Contains_Defer_Directive_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("Endpoint 'GetUser' contains the '@defer' directive, which is not allowed in OpenAPI definitions.", error.Message);
    }

    [Fact]
    public async Task Endpoint_Contains_Stream_Directive_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("Endpoint 'GetUsers' contains the '@stream' directive, which is not allowed in OpenAPI definitions.", error.Message);
    }

    #endregion
}
