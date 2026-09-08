using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed partial class PolicySlotGatewayTests
{
    // Ruling repo-ctf.24 (comment 704): the built-in fusion.authenticated and
    // fusion.scope:<scope> policies are registered by default; no IPolicyProvider needs to be
    // configured on the gateway for them to take effect.
    [Fact]
    public async Task ExecuteAsync_Should_DenyWithError_When_AuthenticatedPolicyDeniesAnonymousUser()
    {
        // arrange
        var executor = await CreateBuiltInPolicyExecutorAsync(
            CreateSchema(
                """
                type Query {
                  secret: String @policy(names: "fusion.authenticated", onDenied: ERROR)
                }
                """),
            new RecordingClient("""{"data":{"secret":"classified"}}"""));
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(new ClaimsPrincipal(new ClaimsIdentity()))
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        NormalizeReasonId(result.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "secret"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": {
                "secret": null
              }
            }
            """);
    }

    // Ruling repo-ctf.24 (comment 734, F1/F4): the built-in fusion.deny policy always denies,
    // regardless of authentication state, once it is referenced by a field.
    [Fact]
    public async Task ExecuteAsync_Should_DenyWithError_When_DenyPolicyIsReferencedAndUserIsAuthenticated()
    {
        // arrange
        var executor = await CreateBuiltInPolicyExecutorAsync(
            CreateSchema(
                """
                type Query {
                  secret: String @policy(names: "fusion.deny", onDenied: ERROR)
                }
                """),
            new RecordingClient("""{"data":{"secret":"classified"}}"""));
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(new ClaimsPrincipal(new ClaimsIdentity([], "test")))
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        NormalizeReasonId(result.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "secret"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": {
                "secret": null
              }
            }
            """);
    }

    // Ruling repo-ctf.24 (comment 734, F4): @requiresScopes(scopes: [[]]) (one empty inner group)
    // translates to the authenticated application only, with no scope application at all. This
    // proves that shape's runtime behavior end to end: an authenticated user is allowed.
    [Fact]
    public async Task ExecuteAsync_Should_Allow_When_AuthenticatedOnlyScopePolicyAndUserIsAuthenticated()
    {
        // arrange
        var executor = await CreateBuiltInPolicyExecutorAsync(
            CreateSchema(
                """
                type Query {
                  secret: String @policy(names: [["fusion.authenticated"]], onDenied: ERROR)
                }
                """),
            new RecordingClient("""{"data":{"secret":"classified"}}"""));
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(new ClaimsPrincipal(new ClaimsIdentity([], "test")))
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
    }

    // Ruling repo-ctf.24 (comment 734, F4): the same authenticated-only shape denies an
    // anonymous user, mirroring Apollo's own @requiresScopes(scopes: [[]]) semantics (AND over
    // zero scopes is satisfied by any subject, but the subject must still be authenticated).
    [Fact]
    public async Task ExecuteAsync_Should_DenyWithError_When_AuthenticatedOnlyScopePolicyDeniesAnonymousUser()
    {
        // arrange
        var executor = await CreateBuiltInPolicyExecutorAsync(
            CreateSchema(
                """
                type Query {
                  secret: String @policy(names: [["fusion.authenticated"]], onDenied: ERROR)
                }
                """),
            new RecordingClient("""{"data":{"secret":"classified"}}"""));
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(new ClaimsPrincipal(new ClaimsIdentity()))
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        NormalizeReasonId(result.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "secret"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": {
                "secret": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Allow_When_AuthenticatedPolicyAndUserIsAuthenticated()
    {
        // arrange
        var executor = await CreateBuiltInPolicyExecutorAsync(
            CreateSchema(
                """
                type Query {
                  secret: String @policy(names: "fusion.authenticated", onDenied: ERROR)
                }
                """),
            new RecordingClient("""{"data":{"secret":"classified"}}"""));
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(new ClaimsPrincipal(new ClaimsIdentity([], "test")))
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Allow_When_ScopePolicyMatchesUserScopeClaim()
    {
        // arrange
        var executor = await CreateBuiltInPolicyExecutorAsync(
            CreateSchema(
                """
                type Query {
                  secret: String @policy(names: "fusion.scope:read:secret", onDenied: ERROR)
                }
                """),
            new RecordingClient("""{"data":{"secret":"classified"}}"""));
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim("scope", "read:secret")], "test")))
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DenyWithError_When_ScopePolicyDoesNotMatchUserScopeClaim()
    {
        // arrange
        var executor = await CreateBuiltInPolicyExecutorAsync(
            CreateSchema(
                """
                type Query {
                  secret: String @policy(names: "fusion.scope:read:secret", onDenied: ERROR)
                }
                """),
            new RecordingClient("""{"data":{"secret":"classified"}}"""));
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim("scope", "read:other")], "test")))
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        NormalizeReasonId(result.ToJson()).MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "secret"
                  ],
                  "extensions": {
                    "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                    "reasonId": "00000000-0000-0000-0000-000000000000"
                  }
                }
              ],
              "data": {
                "secret": null
              }
            }
            """);
    }

    // Regression for repo-ctf.24 fix cycle 2 (comment 734, F3): the built-in fusion.scope:<scope>
    // policy must read whatever claim types FusionOptions.ScopeClaimTypes configures, not just the
    // default scope/scp pair.
    [Fact]
    public async Task ExecuteAsync_Should_Allow_When_ScopePolicyMatchesCustomClaimType()
    {
        // arrange
        var executor = await CreateBuiltInPolicyExecutorAsync(
            CreateSchema(
                """
                type Query {
                  secret: String @policy(names: "fusion.scope:read:secret", onDenied: ERROR)
                }
                """),
            new RecordingClient("""{"data":{"secret":"classified"}}"""),
            setup => setup.OptionsModifiers.Add(options => options.ScopeClaimTypes = ["permissions"]));
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim("permissions", "read:secret")], "test")))
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
    }

    // Ruling repo-ctf.24: a user policy with the same name as a built-in wins.
    [Fact]
    public async Task ExecuteAsync_Should_LetUserPolicyOverrideBuiltIn_When_NamesCollide()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query {
                  secret: String @policy(names: "fusion.authenticated", onDenied: ERROR)
                }
                """),
            new AllowPolicy("fusion.authenticated"),
            new RecordingClient("""{"data":{"secret":"classified"}}"""));
        var request = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(new ClaimsPrincipal(new ClaimsIdentity()))
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert: the anonymous user would be denied by the built-in fusion.authenticated
        // policy, but the user-registered override always allows.
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
    }

    private static async Task<IRequestExecutor> CreateBuiltInPolicyExecutorAsync(
        string schema,
        RecordingClient client,
        Action<FusionGatewaySetup>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var builder = services.AddGraphQLGateway();
        builder.AddInMemoryConfiguration(ComposeSchemaDocument(schema));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new ClientFactory(("a", client)));
        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(_ => new ClientConfiguration("a")));

        if (configure is not null)
        {
            FusionSetupUtilities.Configure(builder, configure);
        }

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private sealed class AllowPolicy(string name) : IPolicy
    {
        public string Name { get; } = name;

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
