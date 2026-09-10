using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Authorization;

public class BatchResolverAuthorizationTests
{
    // REPRO (security): [Authorize] (default BeforeResolver) on a [BatchResolver] field is
    // silently bypassed because the auth directive middleware lives only in the regular
    // pipeline which a batch-strategy selection never runs. An unauthorized user must get an
    // AUTH_NOT_AUTHORIZED error and null data, not the protected secret values.
    [Fact]
    public async Task Authorize_BeforeResolver_On_Batch_Field_Should_Deny_When_NotAllowed()
    {
        // arrange
        SecretQuery.ResolverInvoked = false;
        var handler = new AuthHandler(
            resolver: (_, d) => d.Policy.EqualsOrdinal("READ_SECRET")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var executor = await CreateExecutorAsync<SecretQuery>(handler);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              a: secretById(id: 1) { value }
              b: secretById(id: 2) { value }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.False(SecretQuery.ResolverInvoked);
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "a"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                },
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "b"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "a": null,
                "b": null
              }
            }
            """);
    }

    // REPRO (security): AfterResolver mode on a [BatchResolver] field is bypassed the same way
    // as BeforeResolver. An unauthorized user must get an AUTH_NOT_AUTHORIZED error and null
    // data instead of the protected secret values.
    [Fact]
    public async Task Authorize_AfterResolver_On_Batch_Field_Should_Deny_When_NotAllowed()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, d) => d.Policy.EqualsOrdinal("READ_SECRET_AFTER")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var executor = await CreateExecutorAsync<SecretAfterQuery>(handler);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              a: secretAfterById(id: 1) { value }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "a"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "a": null
              }
            }
            """);
    }

    // REPRO (security): [Authorize] on an object type returned by a [BatchResolver] field is
    // bypassed because the type-injected auth middleware lands in the regular pipeline which the
    // batch strategy never executes. An unauthorized user must not receive Person instances.
    [Fact]
    public async Task Authorize_On_Type_Returned_By_Batch_Field_Should_Deny_When_NotAllowed()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, d) => d.Policy.EqualsOrdinal("READ_FRIEND")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FriendsQuery>()
            .AddTypeExtension<AppUserExtensions>()
            .AddAuthorizationHandler(_ => handler)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              users {
                friend(key: "a") { id }
              }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "users",
                    0,
                    "friend"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "users": [
                  {
                    "friend": null
                  }
                ]
              }
            }
            """);
    }

    // REPRO (functional break): a node type annotated [Authorize] whose node resolver is a batch
    // [NodeResolver] field must remain fetchable through node(id:) for AUTHORIZED users. Today the
    // auth interceptor rebuilds NodeResolverInfo and drops the batch pipeline, so the node is
    // never dispatched and the authorized user silently gets node: null with no error instead of
    // the node.
    [Fact]
    public async Task Authorize_On_Batch_Node_Type_Should_Resolve_Node_When_Allowed()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, _) => AuthorizeResult.Allowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var executor = await CreateNodeExecutorAsync(handler);
        var id = Convert.ToBase64String("ProtectedPerson:abc"u8);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($id: ID!) {
                      node(id: $id) {
                        __typename
                      }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { { "id", id } })
                .Build(),
                cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "__typename": "ProtectedPerson"
                }
              }
            }
            """);
    }

    // REPRO (security): the unauthorized half of the batch [NodeResolver] type-policy case. A
    // denied user must get AUTH_NOT_AUTHORIZED with node: null when fetching a protected batch
    // node through node(id:).
    [Fact]
    public async Task Authorize_On_Batch_Node_Type_Should_Deny_Node_When_NotAllowed()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, d) => d.Policy.EqualsOrdinal("READ_PROTECTED_PERSON")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var executor = await CreateNodeExecutorAsync(handler);
        var id = Convert.ToBase64String("ProtectedPerson:abc"u8);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($id: ID!) {
                      node(id: $id) {
                        __typename
                      }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { { "id", id } })
                .Build(),
                cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "node"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "node": null
              }
            }
            """);
    }

    // PROVES-WORKS: [Authorize(ApplyPolicy.Validation)] on a [BatchResolver] field is enforced
    // because validation-mode auth runs during document validation, independent of the execution
    // strategy. This is the supported workaround until execution-mode auth works on batch fields.
    [Fact]
    public async Task Authorize_Validation_On_Batch_Field_Should_Deny_When_NotAllowed()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, _) => AuthorizeResult.Allowed,
            validation: (_, d) => d.Policy.EqualsOrdinal("READ_THING")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed);
        var executor = await CreateExecutorAsync<ThingQuery>(handler);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              a: thingById(id: 1) { id }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ]
            }
            """);

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData!.TryGetValue(ExecutionContextData.HttpStatusCode, out var value));
        Assert.Equal(401, value);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync<TQuery>(
        IAuthorizationHandler handler)
        where TQuery : class
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<TQuery>()
            .AddAuthorizationHandler(_ => handler)
            .BuildRequestExecutorAsync();

    private static async Task<IRequestExecutor> CreateNodeExecutorAsync(
        IAuthorizationHandler handler)
        => await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<NodeQuery>()
            .AddType<ProtectedPerson>()
            .AddGlobalObjectIdentification()
            .AddAuthorizationHandler(_ => handler)
            .BuildRequestExecutorAsync();

    public sealed record Secret(int Id, string Value);

    public sealed class SecretQuery
    {
        public static bool ResolverInvoked { get; set; }

        [BatchResolver]
        [Authorize("READ_SECRET", ApplyPolicy.BeforeResolver)]
        public List<Secret> GetSecretById(List<int> id)
        {
            ResolverInvoked = true;
            return id.Select(i => new Secret(i, $"secret-{i}")).ToList();
        }
    }

    public sealed class SecretAfterQuery
    {
        [BatchResolver]
        [Authorize("READ_SECRET_AFTER", ApplyPolicy.AfterResolver)]
        public List<Secret> GetSecretAfterById(List<int> id)
            => id.Select(i => new Secret(i, $"secret-{i}")).ToList();
    }

    [Authorize("READ_FRIEND", ApplyPolicy.BeforeResolver)]
    public sealed record Person(string Id);

    public sealed record AppUser(string Id);

    public sealed class FriendsQuery
    {
        public List<AppUser> GetUsers() => [new AppUser("u1")];
    }

    [ExtendObjectType<AppUser>]
    public sealed class AppUserExtensions
    {
        [BatchResolver]
        public List<Person> GetFriend(List<string> key)
            => key.Select(i => new Person(i)).ToList();
    }

    [Authorize("READ_PROTECTED_PERSON", ApplyPolicy.BeforeResolver)]
    public sealed class ProtectedPerson
    {
        public required string Id { get; set; }
    }

    public sealed class NodeQuery
    {
        [NodeResolver]
        [BatchResolver]
        public List<ProtectedPerson> GetProtectedPersonById(List<string> id)
            => id.Select(i => new ProtectedPerson { Id = i }).ToList();
    }

    public sealed record Thing(int Id);

    public sealed class ThingQuery
    {
        [BatchResolver]
        [Authorize("READ_THING", ApplyPolicy.Validation)]
        public List<Thing> GetThingById(List<int> id)
            => id.Select(i => new Thing(i)).ToList();
    }

    private sealed class AuthHandler : IAuthorizationHandler
    {
        private readonly Func<IMiddlewareContext, AuthorizeDirective, AuthorizeResult> _resolver;

        private readonly Func<AuthorizationContext, AuthorizeDirective, AuthorizeResult>
            _validation;

        public AuthHandler(
            Func<IMiddlewareContext, AuthorizeDirective, AuthorizeResult> resolver,
            Func<AuthorizationContext, AuthorizeDirective, AuthorizeResult> validation)
        {
            _resolver = resolver;
            _validation = validation;
        }

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken = default)
            => new(_resolver(context, directive));

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken = default)
        {
            foreach (var directive in directives)
            {
                var result = _validation(context, directive);

                if (result is not AuthorizeResult.Allowed)
                {
                    return new ValueTask<AuthorizeResult>(result);
                }
            }

            return new(AuthorizeResult.Allowed);
        }
    }
}
