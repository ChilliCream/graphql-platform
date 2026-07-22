using System.Security.Claims;
using System.Text;
using ChilliCream.Regorus;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class RegoPolicyTests
{
    private static readonly ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> s_fieldMapPool =
        new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new FieldMapPooledObjectPolicy());

    [Fact]
    public async Task EvaluateAsync_Should_UsePolicyData_When_BatchContainsPermissionLookups()
    {
        // arrange
        var requirements = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }");
        using var policy = CreatePolicy(
            requirements,
            """
            {"allow": [object.get(data.permissions.product_readers, entity.id, false) |
                some entity in input.entities]}
            """,
            """
            {
              "permissions": {
                "product_readers": {
                  "1": true
                }
              }
            }
            """);
        using var first = CreateEntity("1", "first", "hidden-1");
        using var second = CreateEntity("2", "second", "hidden-2");
        var context = new TestPolicyContext();
        var entities = new[] { first.Data, second.Data };

        // act
        await policy.EvaluateAsync(
            context,
            entities,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([1], context.DeniedIndices);
    }

    [Fact]
    public async Task EvaluateAsync_Should_ProjectOnlyRequiredFields_When_EntityContainsAdditionalData()
    {
        // arrange
        var requirements = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id nested { code } }");
        using var policy = CreatePolicy(
            requirements,
            """
            {"allow": [input.entities[0] == {"id": "1", "nested": {"code": "visible"}}]}
            """);
        using var entity = CreateEntity("1", "visible", "hidden");
        var context = new TestPolicyContext();
        var entities = new[] { entity.Data };

        // act
        await policy.EvaluateAsync(
            context,
            entities,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(context.DeniedIndices);
    }

    [Fact]
    public async Task EvaluateAsync_Should_NotReadEntities_When_RequirementsAreNull()
    {
        // arrange
        using var policy = CreatePolicy(requirements: null, """{"allow": [false]}""");
        var context = new TestPolicyContext();
        var entities = new CompositeResultElement[1];

        // act
        await policy.EvaluateAsync(
            context,
            entities,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([0], context.DeniedIndices);
    }

    [Fact]
    public async Task EvaluateAsync_Should_Throw_When_ResultLengthDoesNotMatchEntityCount()
    {
        // arrange
        using var policy = CreatePolicy(requirements: null, """{"allow": [true]}""");
        var context = new TestPolicyContext();
        var entities = new CompositeResultElement[2];

        // act
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => policy.EvaluateAsync(
                context,
                entities,
                TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.Equal(
            "Rego authorization policy 'CanReadProduct' returned an invalid decision.",
            error.Message);
        Assert.Empty(context.DeniedIndices);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("{\"allow\": true}")]
    [InlineData("{\"allow\": [\"true\"]}")]
    [InlineData("{\"other\": [true]}")]
    public async Task EvaluateAsync_Should_Throw_When_ResultShapeIsInvalid(string decision)
    {
        // arrange
        using var policy = CreatePolicy(requirements: null, decision);
        var context = new TestPolicyContext();
        var entities = new CompositeResultElement[1];

        // act
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => policy.EvaluateAsync(
                context,
                entities,
                TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.Equal(
            "Rego authorization policy 'CanReadProduct' returned an invalid decision.",
            error.Message);
        Assert.Empty(context.DeniedIndices);
    }

    [Fact]
    public async Task Dispose_Should_KeepPolicyAlive_When_StillPinned()
    {
        // arrange
        var policy = CreatePolicy(requirements: null, """{"allow": [true]}""");
        Assert.True(((IPolicyLifetime)policy).TryAddRef());
        var context = new TestPolicyContext();
        var entities = new CompositeResultElement[1];

        // act
        // A direct dispose drops only the caller's reference; the outstanding pin keeps the native
        // policy alive so an in-flight evaluation is never freed underneath it.
        policy.Dispose();

        // assert
        await policy.EvaluateAsync(
            context,
            entities,
            TestContext.Current.CancellationToken);
        Assert.Empty(context.DeniedIndices);

        ((IPolicyLifetime)policy).Release();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => policy.EvaluateAsync(
                context,
                entities,
                TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task EvaluateAsync_Should_Throw_When_PolicyIsDisposed()
    {
        // arrange
        var policy = CreatePolicy(requirements: null, """{"allow": [true]}""");
        policy.Dispose();
        var context = new TestPolicyContext();
        var entities = new CompositeResultElement[1];

        // act
        var error = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => policy.EvaluateAsync(
                context,
                entities,
                TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.Equal(typeof(RegoPolicy).FullName, error.ObjectName);
    }

    private static RegoPolicy CreatePolicy(
        SelectionSetNode? requirements,
        string decision)
        => CreatePolicy(requirements, decision, "{}");

    private static RegoPolicy CreatePolicy(
        SelectionSetNode? requirements,
        string decision,
        string dataJson)
    {
        var source = $$"""
            package fusion_test
            import rego.v1

            decision := {{decision}}
            """;

        return new RegoPolicy(
            "CanReadProduct",
            requirements,
            Encoding.UTF8.GetBytes(dataJson),
            [new PolicyModule("fusion_test.rego", source)],
            "data.fusion_test.decision");
    }

    private static CompositeResultDocument CreateEntity(
        string id,
        string code,
        string extra)
    {
        var schema = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(
                """
                schema {
                  query: Query
                }

                type Query @fusion__type(schema: A) {
                  id: ID @fusion__field(schema: A)
                  nested: Nested @fusion__field(schema: A)
                  extra: String @fusion__field(schema: A)
                }

                type Nested @fusion__type(schema: A) {
                  code: String @fusion__field(schema: A)
                  extra: String @fusion__field(schema: A)
                }

                enum fusion__Schema {
                  A @fusion__schema_metadata(name: "A")
                }
                """),
            new ServiceCollection().BuildServiceProvider());
        var operationDefinition = Utf8GraphQLParser.Parse(
                """
                {
                  id
                  nested {
                    code
                    extra
                  }
                  extra
                }
                """)
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
        var operation = new OperationCompiler(schema, s_fieldMapPool)
            .Compile("test", "test", operationDefinition);
        var result = new CompositeResultDocument(
            CommonTestExtensions.CreateArena(),
            operation,
            includeFlags: 0);
        var nested = result.Data.GetProperty("nested");
        nested.SetObjectValue(operation.GetSelectionSet(nested.AssertSelection()));

        var sourceBytes = Encoding.UTF8.GetBytes(
            $$"""
            {
              "id": "{{id}}",
              "nested": {
                "code": "{{code}}",
                "extra": "{{extra}}"
              },
              "extra": "{{extra}}"
            }
            """);
        var source = SourceResultDocument.Parse(
            CommonTestExtensions.CreateArena(),
            sourceBytes,
            sourceBytes.Length);

        result.Data.GetProperty("id").SetLeafValue(source.Root.GetProperty("id"));
        result.Data.GetProperty("extra").SetLeafValue(source.Root.GetProperty("extra"));
        nested.GetProperty("code").SetLeafValue(source.Root.GetProperty("nested").GetProperty("code"));
        nested.GetProperty("extra").SetLeafValue(source.Root.GetProperty("nested").GetProperty("extra"));

        return result;
    }

    private sealed class TestPolicyContext : IPolicyContext
    {
        public List<int> DeniedIndices { get; } = [];

        public ISelection? Selection => null;

        public ITypeDefinition Type => throw new NotSupportedException();

        public PolicyDenialBehavior OnDenied => PolicyDenialBehavior.Null;

        public ClaimsPrincipal User { get; } = new();

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public void Deny(int index, string? reason = null)
            => DeniedIndices.Add(index);
    }
}
