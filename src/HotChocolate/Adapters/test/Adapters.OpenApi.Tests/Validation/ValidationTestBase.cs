using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

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
        Assert.Equal("Model contains the '@defer' directive, which is not supported for OpenAPI models.", error.Message);
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
        Assert.Equal("Model contains the '@stream' directive, which is not supported for OpenAPI models.", error.Message);
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
        Assert.Equal("Endpoint operation type is 'subscription', but only 'query' and 'mutation' are supported.", error.Message);
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
        Assert.Equal("Endpoint must select exactly one root field.", error.Message);
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
        Assert.Equal("Endpoint must select exactly one root field.", error.Message);
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
        Assert.Equal("Endpoint has 2 parameters mapping to the same variable(-path) '$userId'. Each variable(-path) can only be mapped once.", error.Message);
    }

    [Fact]
    public async Task Endpoint_Parameter_Conflict_BodyVariable_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUser($userId: ID! @body) @http(method: GET, route: "/users/{userId}") {
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
        Assert.Equal("Endpoint has 2 parameters mapping to the same variable(-path) '$userId'. Each variable(-path) can only be mapped once.", error.Message);
    }

    [Fact]
    public async Task Endpoint_Parameter_Conflict_Same_InputObjectPath_RaisesError()
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
        Assert.Equal("Endpoint has 2 parameters mapping to the same variable(-path) '$user.id'. Each variable(-path) can only be mapped once.", error.Message);
    }

    [Fact]
    public async Task Endpoint_RouteParameter_ReferencesNonExistentVariable_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users/{nonExistent}") {
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
        Assert.Equal("Route parameter 'nonExistent' references variable '$nonExistent' which does not exist in the operation.", error.Message);
    }

    [Fact]
    public async Task Endpoint_RouteParameter_Has_Invalid_InputObjectPath_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
            """
            mutation UpdateUser($user: UserInput! @body) @http(method: PUT, route: "/users/{userId:$user.userId}") {
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
        Assert.Equal("Route parameter 'userId' has an invalid input object path 'userId' for variable '$user'.", error.Message);
    }

    [Fact]
    public async Task Endpoint_QueryParameter_ReferencesNonExistentVariable_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
            """
            query GetUser($userId: ID!) @http(method: GET, route: "/users", queryParameters: ["nonExistent"]) {
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
        Assert.Equal("Query parameter 'nonExistent' references variable '$nonExistent' which does not exist in the operation.", error.Message);
    }

    [Fact]
    public async Task Endpoint_QueryParameter_Has_Invalid_InputObjectPath_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage(
            """
            mutation UpdateUser($user: UserInput! @body) @http(method: PUT, route: "/users", queryParameters: ["userId:$user.userId"]) {
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
        Assert.Equal("Query parameter 'userId' has an invalid input object path 'userId' for variable '$user'.", error.Message);
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
        Assert.Equal("Endpoint contains the '@defer' directive, which is not supported for OpenAPI endpoints.", error.Message);
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
        Assert.Equal("Endpoint contains the '@stream' directive, which is not supported for OpenAPI endpoints.", error.Message);
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
        Assert.Equal("Endpoint is missing a named GraphQL operation. Anonymous operations are not supported.", error.Message);
    }

    [Theory]
    // No leading slash
    [InlineData("api/users")]
    // No trailing slash
    [InlineData("/api/")]
    // Not just root segment
    [InlineData("/")]
    // No mapping syntax
    [InlineData("/api/users/{userId:int}")]
    // Spaces
    [InlineData("/api/ users")]
    // Catch all
    [InlineData("/api/{**catchAll}")]
    // Optional parameters
    [InlineData("/api/{status?}")]
    public async Task Endpoint_InvalidRoute_RaisesError(string route)
    {
        // arrange
        using var cts = new CancellationTokenSource(s_testTimeout);
        var storage = new TestOpenApiDefinitionStorage();
        var definition = OpenApiEndpointDefinition.From(
            new OpenApiEndpointSettingsDto(
                null,
                [],
                [],
                null),
            "GET",
            route,
            Utf8GraphQLParser.Parse(
                """
                  query GetUsers {
                    users {
                      id
                    }
                  }
                  """));
        storage.AddOrUpdateDefinition("1", definition);
        var eventListener = new TestOpenApiDiagnosticEventListener();
        var server = CreateTestServer(storage, eventListener);

        // act
        await server.Services.GetRequestExecutorAsync(cancellationToken: cts.Token);

        eventListener.HasReportedErrors.Wait(cts.Token);

        // assert
        var error = Assert.Single(eventListener.Errors);
        Assert.Equal($"Endpoint has invalid route pattern '{route}'.", error.Message);
    }

    #endregion
}
