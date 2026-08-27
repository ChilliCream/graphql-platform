using System.Security.Claims;
using System.Text;
using ChilliCream.Regorus;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class RegoPolicyTests
{
    private const string DenyAll =
        """
        package fusion_test
        import rego.v1

        default allow := false
        """;

    [Fact]
    public async Task EvaluateAsync_Should_DenyEntity_When_DataDoesNotGrantAccess()
    {
        // arrange
        var requirements = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }");
        var (policy, handle) = CreatePolicy(
            requirements,
            """
            package fusion_test
            import rego.v1

            default allow := false
            allow if { data.permissions.product_readers[input.resource.id] }
            """,
            """{ "permissions": { "product_readers": { "1": true } } }""");
        using var first = RegoPolicyTestEntities.CreateEntity("1", "first", "hidden-1");
        using var second = RegoPolicyTestEntities.CreateEntity("2", "second", "hidden-2");
        var entities = new[] { first.Data, second.Data };
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: entities);

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([1], context.DeniedIndices);
        handle.Dispose();
    }

    [Fact]
    public async Task EvaluateAsync_Should_ProjectOnlyRequiredFields_When_EntityContainsAdditionalData()
    {
        // arrange
        var requirements = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id nested { code } }");
        var (policy, handle) = CreatePolicy(
            requirements,
            """
            package fusion_test
            import rego.v1

            default allow := false
            allow if { input.resource == {"id": "1", "nested": {"code": "visible"}} }
            """);
        using var entity = RegoPolicyTestEntities.CreateEntity("1", "visible", "hidden");
        var entities = new[] { entity.Data };
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: entities);

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(context.DeniedIndices);
        handle.Dispose();
    }

    [Fact]
    public async Task EvaluateAsync_Should_DenyEntity_When_RequirementsAreNull()
    {
        // arrange
        var (policy, handle) = CreatePolicy(requirements: null, DenyAll);
        var entities = new CompositeResultElement[1];
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: entities);

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([0], context.DeniedIndices);
        handle.Dispose();
    }

    [Fact]
    public async Task EvaluateAsync_Should_DenyWithReason_When_PolicyProducesUndefinedDecision()
    {
        // arrange
        // The rule has no default, so a non-matching body yields an undefined decision.
        var requirements = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }");
        var (policy, handle) = CreatePolicy(
            requirements,
            """
            package fusion_test
            import rego.v1

            allow if { input.resource.id == "unreachable" }
            """);
        using var entity = RegoPolicyTestEntities.CreateEntity("1", "code", "extra");
        var entities = new[] { entity.Data };
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: entities);

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal((0, "The policy did not produce a decision."), Assert.Single(context.Denials));
        handle.Dispose();
    }

    [Fact]
    public async Task EvaluateAsync_Should_OmitResource_When_RequirementsAreNull()
    {
        // arrange
        // The rule allows only when the input carries no resource part, so it passes exactly when a
        // request level policy that declares no resource requirement omits the resource.
        var (policy, handle) = CreatePolicy(
            requirements: null,
            """
            package fusion_test
            import rego.v1

            default allow := false
            allow if { not input.resource }
            """);
        var entities = new CompositeResultElement[1];
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: entities);

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(context.DeniedIndices);
        handle.Dispose();
    }

    [Fact]
    public async Task EvaluateAsync_Should_AllowEntity_When_SubjectCarriesRequiredRole()
    {
        // arrange
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Role, "product-reader")], "test"));
        var (policy, handle) = CreatePolicy(
            requirements: null,
            """
            package fusion_test
            import rego.v1

            default allow := false
            allow if { "product-reader" in input.subject.roles }
            """);
        var entities = new CompositeResultElement[1];
        var context = new RegoPolicyTestEntities.TestPolicyContext(user, entities: entities);

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(context.DeniedIndices);
        handle.Dispose();
    }

    private static (RegoPolicy Policy, PolicySetHandle Handle) CreatePolicy(
        SelectionSetNode? requirements,
        string rego)
        => CreatePolicy(requirements, rego, "{}");

    private static (RegoPolicy Policy, PolicySetHandle Handle) CreatePolicy(
        SelectionSetNode? requirements,
        string rego,
        string dataJson)
    {
        const string entryPoint = "data.fusion_test.allow";
        var set = CompiledPolicySet.Compile(
            Encoding.UTF8.GetBytes(dataJson),
            [new PolicyModule("fusion_test.rego", rego)],
            [entryPoint]);
        var handle = new PolicySetHandle(set);
        var policy = new RegoPolicy(
            "CanReadProduct.allow",
            requirements is null
                ? PolicyRequirements.Empty
                : new PolicyRequirements { Resource = requirements },
            handle,
            set.GetEntryPointIndex(entryPoint));
        return (policy, handle);
    }
}
