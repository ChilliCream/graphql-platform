using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Policies.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Policies;

public abstract class PolicyExecutionTestBase : FusionTestBase
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

    internal static Task<IRequestExecutor> CreateExecutorAsync(
        string sourceSchema,
        PolicyTestClient client,
        InMemoryPolicyRecorder recorder,
        Action<InMemoryPolicyBuilder>? policies = null,
        Action<IFusionGatewayBuilder>? configure = null)
        => CreateExecutorAsync([sourceSchema], client, recorder, policies, configure);

    internal static async Task<IRequestExecutor> CreateExecutorAsync(
        string[] sourceSchemas,
        PolicyTestClient client,
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

        builder.AddInMemoryConfiguration(
            ComposeSchemaDocument(
                defaultListSize: 1,
                [.. sourceSchemas.Select(static sourceSchema => sourceSchema + "\n" + Directives)]));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new PolicyTestClientFactory(client));
        builder.ConfigureSchemaServices(
            (_, sc) => sc.AddSingleton<IAuthenticationSchemeLookup>(
                new TestAuthenticationSchemeLookup("Bearer") { Challenges = s_challenges }));

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                for (var i = 0; i < sourceSchemas.Length; i++)
                {
                    var name = ((char)('a' + i)).ToString();
                    setup.ClientConfigurationModifiers.Add(_ => new PolicyTestClientConfiguration(name));
                }
            });

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
