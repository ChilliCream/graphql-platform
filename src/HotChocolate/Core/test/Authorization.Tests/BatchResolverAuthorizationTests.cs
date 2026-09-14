using System.Collections.Concurrent;
using System.Security.Claims;
using CookieCrumble;
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

    [Theory]
    [InlineData(ApplyPolicy.BeforeResolver, "Cached")]
    [InlineData(ApplyPolicy.AfterResolver, "Cached")]
    [InlineData(ApplyPolicy.BeforeResolver, "Null")]
    [InlineData(ApplyPolicy.AfterResolver, "Null")]
    [InlineData(ApplyPolicy.BeforeResolver, "Error")]
    [InlineData(ApplyPolicy.AfterResolver, "Error")]
    [InlineData(ApplyPolicy.AfterResolver, "ErrorThrows")]
    [InlineData(ApplyPolicy.BeforeResolver, "Reported")]
    [InlineData(ApplyPolicy.AfterResolver, "Reported")]
    [InlineData(ApplyPolicy.BeforeResolver, "Throws")]
    [InlineData(ApplyPolicy.AfterResolver, "Throws")]
    public async Task Authorize_Should_PreserveEntryOwnership_When_MiddlewareShortCircuits(
        ApplyPolicy apply,
        string seed)
    {
        // arrange
        var calls = new List<string>();
        var batches = new List<int[]>();
        var observed = new List<int>();
        var handler = new AuthHandler((context, directive) =>
        {
            var id = context.Parent<Secret>().Id;
            calls.Add($"{directive.Policy}:{id}");
            if (seed is "Throws" or "ErrorThrows" && id == 2)
            {
                throw new InvalidOperationException("handler failed");
            }

            return id == 2 ? AuthorizeResult.NotAllowed : AuthorizeResult.Allowed;
        }, (_, _) => AuthorizeResult.Allowed);
        var executor = await new ServiceCollection().AddGraphQL()
            .AddAuthorizationHandler(_ => handler)
            .AddQueryType(d => d.Field("parents")
                .Type<ListType<ObjectType<Secret>>>()
                .Resolve(new[] { new Secret(1, "one"), new Secret(2, "two"), new Secret(3, "three") }))
            .AddObjectType<Secret>(d =>
            {
                var field = d.Field("secured").Type<ObjectType<EntrySecret>>()
                    .ResolveBatch(contexts =>
                    {
                        batches.Add(contexts.Select(c => c.Parent<Secret>().Id).ToArray());
                        return new ValueTask<IReadOnlyList<ResolverResult>>(contexts
                            .Select(c => ResolverResult.Ok(new EntrySecret(c.Parent<Secret>().Id)))
                            .ToArray());
                    });
                field.Extend().Configuration.BatchMiddlewareConfigurations.Add(new(
                    next => async contexts =>
                    {
                        foreach (var context in contexts)
                        {
                            if (context.Parent<Secret>().Id == 2)
                            {
                                switch (seed)
                                {
                                    case "Cached":
                                        context.Result = new EntrySecret(2);
                                        break;
                                    case "Null":
                                        context.Result = null;
                                        break;
                                    case "Error":
                                    case "ErrorThrows":
                                        context.Result = ErrorBuilder.New().SetMessage("owned error").SetCode("OWNED").Build();
                                        break;
                                    case "Reported":
                                        context.ReportError(ErrorBuilder.New().SetMessage("owned error").SetCode("OWNED").Build());
                                        break;
                                }
                            }
                        }

                        await next(contexts);
                        observed.Add(contexts.Length);
                    },
                    key: WellKnownMiddleware.Authorization));
            })
            .AddObjectType<EntrySecret>(d => d.Authorize("READ", apply: apply))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ parents { secured { id } } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot(postFix: $"{apply}_{seed}")
            .Add(result, "Result")
            .Add(calls, "Authorization calls")
            .Add(batches, "Resolver batches")
            .Add(observed, "After-next context counts")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(ApplyPolicy.BeforeResolver, false)]
    [InlineData(ApplyPolicy.BeforeResolver, true)]
    [InlineData(ApplyPolicy.AfterResolver, false)]
    [InlineData(ApplyPolicy.AfterResolver, true)]
    public async Task Authorize_Should_EnforceCachedValuePolicy_When_ErrorWasAlreadyReported(
        ApplyPolicy apply,
        bool allowed)
    {
        // arrange
        var calls = new List<int>();
        var dispatches = new List<int[]>();
        var observed = new List<int>();
        var handler = new AuthHandler((context, _) =>
        {
            var id = context.Parent<Secret>().Id;
            calls.Add(id);
            return id == 1 && !allowed ? AuthorizeResult.NotAllowed : AuthorizeResult.Allowed;
        }, (_, _) => AuthorizeResult.Allowed);
        var executor = await new ServiceCollection().AddGraphQL()
            .AddAuthorizationHandler(_ => handler)
            .AddQueryType(d => d.Field("parents").Type<ListType<ObjectType<Secret>>>()
                .Resolve(new[] { new Secret(1, "one"), new Secret(2, "two"), new Secret(3, "three") }))
            .AddObjectType<Secret>(d =>
            {
                var field = d.Field("secured").Type<ObjectType<EntrySecret>>()
                    .ResolveBatch(contexts =>
                    {
                        dispatches.Add(contexts.Select(c => c.Parent<Secret>().Id).ToArray());
                        return new ValueTask<IReadOnlyList<ResolverResult>>(contexts
                            .Select(c => ResolverResult.Ok(new EntrySecret(c.Parent<Secret>().Id)))
                            .ToArray());
                    });
                field.Extend().Configuration.BatchMiddlewareConfigurations.Add(new(
                    next => async contexts =>
                    {
                        foreach (var context in contexts)
                        {
                            if (context.Parent<Secret>().Id == 1)
                            {
                                context.ReportError("recoverable owned error");
                                context.Result = new EntrySecret(101);
                            }
                        }

                        await next(contexts);
                        observed.Add(contexts.Length);
                    }, key: WellKnownMiddleware.Authorization));
            })
            .AddObjectType<EntrySecret>(d => d.Authorize("READ", apply: apply))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ parents { secured { id } } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new[] { 1, 2, 3 }, calls);
        new Snapshot(postFix: $"{apply}_{allowed}")
            .Add(result, "Result")
            .Add(calls, "Authorization calls")
            .Add(dispatches, "Resolver dispatches")
            .Add(observed, "After-next context counts")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Authorize_Should_IsolateDenial_When_VariableSetsShareSelection()
    {
        // arrange
        var calls = new List<string>();
        var batches = new List<int[]>();
        var handler = new AuthHandler((context, directive) =>
        {
            var id = context.ArgumentValue<int>("id");
            calls.Add($"{directive.Policy}:{id}");
            return id == 2 ? AuthorizeResult.NoDefaultPolicy : AuthorizeResult.Allowed;
        }, (_, _) => AuthorizeResult.Allowed);
        var executor = await new ServiceCollection().AddGraphQL()
            .AddAuthorizationHandler(_ => handler)
            .AddQueryType(d =>
            {
                d.Field("secret").Type<StringType>()
                    .Argument("id", a => a.Type<NonNullType<IntType>>())
                    .Authorize("FIRST")
                    .Authorize("SECOND")
                    .ResolveBatch(contexts =>
                    {
                        batches.Add(contexts.Select(c => c.ArgumentValue<int>("id")).ToArray());
                        return new ValueTask<IReadOnlyList<ResolverResult>>(contexts
                            .Select(c => ResolverResult.Ok($"secret-{c.ArgumentValue<int>("id")}"))
                            .ToArray());
                    });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument("query($id:Int!) { secret(id:$id) }")
            .SetVariableValues(new IReadOnlyDictionary<string, object?>[]
            {
                new Dictionary<string, object?> { ["id"] = 1 },
                new Dictionary<string, object?> { ["id"] = 2 },
                new Dictionary<string, object?> { ["id"] = 3 }
            })
            .Build(), cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        new Snapshot()
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .Add(batch.Results[2], "Set 2")
            .Add(calls, "Authorization calls")
            .Add(batches, "Resolver batches")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Authorize_Should_UseOwningUser_When_RequestsExecuteConcurrently()
    {
        // arrange
        var calls = new ConcurrentQueue<string>();
        var batches = new ConcurrentQueue<string[]>();
        var handler = new AuthHandler((context, _) =>
        {
            var user = context.GetUser()!.Identity!.Name!;
            calls.Enqueue(user);
            return user == "allowed" ? AuthorizeResult.Allowed : AuthorizeResult.NotAuthenticated;
        }, (_, _) => AuthorizeResult.Allowed);
        var executor = await new ServiceCollection().AddGraphQL()
            .AddAuthorizationHandler(_ => handler)
            .AddQueryType(d => d.Field("secret").Type<StringType>().Authorize()
                .ResolveBatch(async contexts =>
                {
                    await Task.Yield();
                    batches.Enqueue(contexts.Select(c => c.GetUser()!.Identity!.Name!).ToArray());
                    return contexts.Select(_ => ResolverResult.Ok("secret")).ToArray();
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var results = await Task.WhenAll(new[] { "allowed", "denied" }.Select(user =>
            executor.ExecuteAsync(OperationRequestBuilder.New().SetDocument("{ secret }")
                .SetUser(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user) }, "test")))
                .Build(), cancellationToken: TestContext.Current.CancellationToken)));

        // assert
        new Snapshot()
            .Add(results[0], "Allowed request")
            .Add(results[1], "Denied request")
            .Add(calls.Order().ToArray(), "Authorization users")
            .Add(batches.ToArray(), "Resolver users")
            .MatchMarkdownSnapshot();
        foreach (var result in results)
        {
            await result.DisposeAsync();
        }
    }

    [Fact]
    public async Task Authorize_Should_ReportPolicyNameAndPath_When_PolicyIsMissing()
    {
        // arrange
        var calls = 0;
        var dispatches = 0;
        var handler = new AuthHandler((_, _) =>
        {
            calls++;
            return AuthorizeResult.PolicyNotFound;
        }, (_, _) => AuthorizeResult.Allowed);
        var executor = await new ServiceCollection().AddGraphQL()
            .AddAuthorizationHandler(_ => handler)
            .AddQueryType(d => d.Field("secret").Type<StringType>().Authorize("MISSING")
                .ResolveBatch(contexts =>
                {
                    dispatches++;
                    return new ValueTask<IReadOnlyList<ResolverResult>>(contexts.Select(_ => ResolverResult.Ok("secret")).ToArray());
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ secret }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(calls, "Authorization calls")
            .Add(dispatches, "Resolver calls").MatchMarkdownSnapshot();
    }

    public sealed record EntrySecret(int Id);

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
        public List<Secret?> GetSecretById(List<int> id)
        {
            ResolverInvoked = true;
            return id.Select(i => (Secret?)new Secret(i, $"secret-{i}")).ToList();
        }
    }

    public sealed class SecretAfterQuery
    {
        [BatchResolver]
        [Authorize("READ_SECRET_AFTER", ApplyPolicy.AfterResolver)]
        public List<Secret?> GetSecretAfterById(List<int> id)
            => id.Select(i => (Secret?)new Secret(i, $"secret-{i}")).ToList();
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
        public List<Person?> GetFriend(List<string> key)
            => key.Select(i => (Person?)new Person(i)).ToList();
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
