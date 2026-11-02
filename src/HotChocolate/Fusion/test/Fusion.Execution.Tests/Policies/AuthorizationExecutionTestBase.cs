using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Policies.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Policies;

public abstract class AuthorizationExecutionTestBase : FusionTestBase
{
    protected const string Directives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    private static readonly ImmutableDictionary<string, string> s_challenges =
        ImmutableDictionary<string, string>.Empty.Add("Bearer", "Bearer");

    internal static async Task<IRequestExecutor> CreateExecutorAsync(
        string sourceSchema,
        AuthorizationTestClient client,
        InMemoryPolicyRecorder recorder,
        Action<InMemoryPolicyBuilder>? policies = null,
        Action<IFusionGatewayBuilder>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton(recorder);

        var builder = services.AddGraphQLGateway();

        // The first provider that knows a policy wins, so the fallback only resolves the
        // names the test does not configure.
        if (policies is not null)
        {
            builder.AddInMemoryPolicies(policies);
        }

        builder.AddInMemoryPolicies(fallback => fallback.Allow("finance").Allow("owner").Allow("editor").Allow("admin"));

        builder.AddInMemoryConfiguration(ComposeSchemaDocument(defaultListSize: 1, sourceSchema + "\n" + Directives));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new AuthorizationTestClientFactory(client));
        builder.ConfigureSchemaServices(
            (_, sc) => sc.AddSingleton<IAuthenticationSchemeLookup>(
                new TestAuthenticationSchemeLookup("Bearer") { Challenges = s_challenges }));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(_ => new AuthorizationTestClientConfiguration("a")));

        configure?.Invoke(builder);

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    internal static OperationRequestBuilder CreateRequest(string document, ClaimsPrincipal? user = null)
    {
        var builder = OperationRequestBuilder.New().SetDocument(document);

        if (user is not null)
        {
            builder.SetUser(user);
        }

        return builder;
    }

    protected static ClaimsPrincipal Authenticated(params Claim[] claims)
        => PolicyTestHelper.Authenticated(claims);

    protected static ClaimsPrincipal Anonymous()
        => PolicyTestHelper.Anonymous();

    protected static async Task<List<string>> ReadPayloadsAsync(
        IExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (result is not ResponseStream stream)
        {
            return [result.ExpectOperationResult().ToJson()];
        }

        var payloads = new List<string>();

        await foreach (var payload in stream.ReadResultsAsync().WithCancellation(cancellationToken))
        {
            payloads.Add(payload.ToJson());
        }

        return payloads;
    }
}
