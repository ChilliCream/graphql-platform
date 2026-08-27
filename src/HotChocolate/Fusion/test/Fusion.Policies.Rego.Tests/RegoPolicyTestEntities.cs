using System.Security.Claims;
using System.Text;
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

internal static class RegoPolicyTestEntities
{
    private static readonly ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> s_fieldMapPool =
        new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new FieldMapPooledObjectPolicy());

    public static CompositeResultDocument CreateEntity(
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

    public sealed class TestPolicyContext : IPolicyContext
    {
        private readonly PolicySelection? _selection;

        public TestPolicyContext(
            ClaimsPrincipal? user = null,
            ISelection? selection = null,
            ReadOnlyMemory<CompositeResultElement> entities = default)
        {
            User = user ?? new ClaimsPrincipal();

            if (!entities.IsEmpty)
            {
                var policySelection = new PolicySelection();
                policySelection.Reset(null!, selection, VariableValueCollection.Empty, entities);
                _selection = policySelection;
            }
        }

        public List<(int Index, string? Reason)> Denials { get; } = [];

        public IReadOnlyList<int> DeniedIndices => [.. Denials.Select(static d => d.Index)];

        public PolicySelection? Selection => _selection;

        public ClaimsPrincipal User { get; }

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public void Deny(int index, string? reason = null)
            => Denials.Add((index, reason));
    }
}
