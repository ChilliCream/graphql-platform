using HotChocolate.Execution;

namespace HotChocolate.Exporters.OpenApi;

// TODO: parameter mismatch with different input paths
public abstract class ValidationTestBase : OpenApiTestBase
{
    [Fact]
    public async Task Operation_Name_Not_Unique_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Contains(
            eventListener.Errors,
            e => e.Message == "Operation name 'getUserById' is already being used by a operation document with the Id '0' ('GetUserById').");
    }

    [Fact]
    public async Task Fragment_Name_Not_Unique_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
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
        Assert.Contains(
            eventListener.Errors,
            e => e.Message.Contains("Fragment name 'User' is already being used"));
    }

    [Fact]
    public async Task Fragment_Reference_Does_Not_Exist_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Contains(
            eventListener.Errors,
            e => e.Message == "Fragment 'NonExistentFragment' referenced by fragment document 'User' does not exist.");
    }

    [Fact]
    public async Task Operation_Fragment_Reference_Does_Not_Exist_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var storage = new TestOpenApiDefinitionStorage(
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
        Assert.Contains(
            eventListener.Errors,
            e => e.Message == "Fragment 'NonExistentFragment' referenced by operation document 'GetUser' does not exist.");
    }

    [Fact]
    public async Task Operation_Is_Subscription_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
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
        Assert.Contains(
            eventListener.Errors,
            e => e.Message == "Operation 'OnUserUpdated' is a subscription. Only queries and mutations are allowed for OpenAPI operations.");
    }

    [Fact]
    public async Task Operation_Multiple_Root_Fields_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
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
        Assert.Contains(
            eventListener.Errors,
            e => e.Message == "Operation 'GetUsers' must have exactly one root field selection, but found 2.");
    }

    [Fact]
    public async Task Operation_Root_Fragment_Spread_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
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
        Assert.Contains(
            eventListener.Errors,
            e => e.Message == "Operation 'GetUser' must have a single root field selection, but found a fragment spread or inline fragment.");
    }

    [Fact]
    public async Task Operation_Parameter_Conflict_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
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
        Assert.Contains(
            eventListener.Errors,
            e => e.Message == "Operation 'GetUser' has conflicting parameters that map to 'userId': route parameter 'userId', query parameter 'userId'.");
    }

    [Fact]
    public async Task Operation_Does_Not_Compile_Against_Schema_RaisesError()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
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
        Assert.Contains(
            eventListener.Errors,
            e => e.Message == "Operation 'GetUser' does not compile against the schema: The field `nonExistentField` does not exist on the type `Query`.");
    }

    private sealed class TestOpenApiDiagnosticEventListener : OpenApiDiagnosticEventListener
    {
        public List<OpenApiValidationError> Errors { get; } = [];

        public ManualResetEventSlim HasReportedErrors { get; } = new(false);

        public override void ValidationErrors(IReadOnlyList<OpenApiValidationError> errors)
        {
            Errors.AddRange(errors);
            HasReportedErrors.Set();
        }
    }
}
