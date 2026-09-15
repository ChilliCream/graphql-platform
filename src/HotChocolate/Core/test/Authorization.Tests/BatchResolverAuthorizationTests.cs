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

    public sealed record Secret(int Id, string Value);

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
