using HotChocolate.Execution;

namespace HotChocolate.Adapters.OpenApi;

public abstract class ValidationTestBase : OpenApiTestBase
{
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromSeconds(5);

    #region Model

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
            "The endpoint must be either a query or mutation.",
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

    [Fact]
    public async Task Endpoint_Without_Operation_Name_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Equal("The endpoint must have an operation name.", error.Message);
    }

    #endregion
}
