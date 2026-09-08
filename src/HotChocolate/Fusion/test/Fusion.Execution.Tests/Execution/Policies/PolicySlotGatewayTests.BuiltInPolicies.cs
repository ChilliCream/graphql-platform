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
        RecordingClient client)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var builder = services.AddGraphQLGateway();
        builder.AddInMemoryConfiguration(ComposeSchemaDocument(schema));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new ClientFactory(("a", client)));
        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(_ => new ClientConfiguration("a")));
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
