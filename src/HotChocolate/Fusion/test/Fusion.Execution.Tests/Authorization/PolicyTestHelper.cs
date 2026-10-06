using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Authorization;

internal static class PolicyTestHelper
{
    public static ISelection[] CreateSelections()
    {
        var schema = OperationCompilerTests.CreateSchema();

        var document = Utf8GraphQLParser.Parse("query { product { id name } }");
        var definition = document.Definitions.OfType<OperationDefinitionNode>().First();
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new FieldMapPooledObjectPolicy());
        var operation = new OperationCompiler(schema, pool).Compile("1", "1", "1", definition);

        var product = operation.RootSelectionSet.Selections[0];
        var productSet = operation.GetSelectionSet(product, product.Type.NamedType<IObjectTypeDefinition>());

        return [product, .. productSet.Selections.ToArray()];
    }

    public static PolicyEvaluationEntry CreateEntry(
        ISelection selection,
        IPolicy policy,
        string directiveName = PolicyDirectiveNames.Policy,
        string? policyName = "p",
        ImmutableArray<ImmutableArray<string>> scopes = default)
        => new(
            new PolicyDescriptor(directiveName, policyName, scopes, selection, policy),
            new Dictionary<string, object?>());

    public static PolicyEvaluationContext CreateContext(
        ClaimsPrincipal user,
        params PolicyEvaluationEntry[] entries)
        => new(
            user,
            new FeatureCollection(),
            new ServiceCollection().BuildServiceProvider(),
            CreatePlan(),
            0,
            entries);

    public static OperationPlan CreatePlan()
        => PlanFactory.Create();

    public static ClaimsPrincipal Authenticated(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "test"));

    public static ClaimsPrincipal Anonymous()
        => new(new ClaimsIdentity());

    public static PolicyOutcome[] Outcomes(PolicyEvaluationContext context)
        => context.Verdicts.ToArray().Select(v => v.Outcome).ToArray();

    private sealed class PlanFactory : FusionTestBase
    {
        public static OperationPlan Create()
        {
            var schema = ComposeSchema(
                """
                # name: a
                type Query {
                  product: Product
                }

                type Product {
                  id: ID!
                  name: String!
                }
                """);

            return PlanOperation(schema, "query GetProduct { product { id name } }");
        }
    }
}
