using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class ShareableFieldRuntimeTypeRoutingPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_IntersectRuntimeTypesAcrossShareableRootProviders()
    {
        var schema = CreateUnionSchema("ApolloFederation", bFieldArguments: null);

        var plan = PlanOperation(schema, UnionQuery);

        Assert.Equal(["Common"], GetRootFragmentTypeNames(plan, "actions"));
    }

    [Fact]
    public void Plan_Should_ExcludeSourceExternalProvider()
    {
        var schema = CreateUnionSchema(
            "ApolloFederation",
            bFieldArguments: ", sourceExternal: true");

        var plan = PlanOperation(schema, UnionQuery);

        Assert.Equal(["Common", "OnlyA"], GetRootFragmentTypeNames(plan, "actions"));
    }

    [Fact]
    public void Plan_Should_UseSourceLocalRuntimeTypesForMixedConnectors()
    {
        var schema = CreateUnionSchema("GraphQL", bFieldArguments: null);

        var plan = PlanOperation(schema, UnionQuery);

        Assert.Equal(["Common", "OnlyA"], GetRootFragmentTypeNames(plan, "actions"));
    }

    [Fact]
    public void Plan_Should_ResetProviderScopeForProvidedField()
    {
        var schema = CreateSchema(
            """
            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              wrapper: Wrapper
                @fusion__field(schema: A, provides: "actions")
                @fusion__field(schema: B)
            }

            type Wrapper @fusion__type(schema: A) @fusion__type(schema: B) {
              actions: [Action!]!
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }

            union Action
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__unionMember(schema: A, member: "Common")
              @fusion__unionMember(schema: A, member: "OnlyA")
              @fusion__unionMember(schema: B, member: "Common")
              @fusion__unionMember(schema: B, member: "OnlyB")
              = Common | OnlyA | OnlyB

            type Common @fusion__type(schema: A) @fusion__type(schema: B) {
              label: String @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type OnlyA @fusion__type(schema: A) {
              a: String @fusion__field(schema: A)
            }

            type OnlyB @fusion__type(schema: B) {
              b: String @fusion__field(schema: B)
            }
            """);

        var plan = PlanOperation(
            schema,
            """
            {
              wrapper {
                actions {
                  ... on Common { label }
                  ... on OnlyA { a }
                  ... on OnlyB { b }
                }
              }
            }
            """);

        Assert.Equal(
            ["Common", "OnlyA"],
            GetRootFragmentTypeNames(plan, "wrapper", "actions"));
    }

    [Fact]
    public void Plan_Should_HonorConcreteSourceTypeNarrowing()
    {
        var schema = CreateSchema(
            """
            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              action: Action
                @fusion__field(schema: A, sourceType: "OnlyA")
                @fusion__field(schema: B)
            }

            interface Action @fusion__type(schema: A) @fusion__type(schema: B) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type OnlyA implements Action
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__implements(schema: A, interface: "Action")
              @fusion__implements(schema: B, interface: "Action") {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              a: String @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type OnlyB implements Action
              @fusion__type(schema: B)
              @fusion__implements(schema: B, interface: "Action") {
              id: ID! @fusion__field(schema: B)
              b: String @fusion__field(schema: B)
            }
            """);

        var plan = PlanOperation(
            schema,
            "{ action { ... on OnlyA { a } ... on OnlyB { b } } }");

        Assert.Equal(["OnlyA"], GetRootFragmentTypeNames(plan, "action"));
    }

    [Fact]
    public void Plan_Should_TreatInterfaceObjectProviderAsWildcard()
    {
        var schema = CreateSchema(
            """
            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              item: Node @fusion__field(schema: A) @fusion__field(schema: B)
            }

            interface Node
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__interfaceObject(schema: B) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type Common implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__field(schema: A)
              label: String @fusion__field(schema: A)
            }

            type OnlyA implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID! @fusion__field(schema: A)
              a: String @fusion__field(schema: A)
            }
            """);

        var plan = PlanOperation(
            schema,
            "{ item { ... on Common { label } ... on OnlyA { a } } }");

        Assert.Equal(["Common", "OnlyA"], GetRootFragmentTypeNames(plan, "item"));
    }

    [Fact]
    public void Plan_Should_ApplyPolicyDifferentialOnKeyedShareablePath()
    {
        var sourceLocalPlan = PlanOperation(
            CreateKeyedSchema(ShareableFieldRuntimeTypeRouting.SourceLocal),
            KeyedWrapperQuery);
        var commonRuntimeTypesPlan = PlanOperation(
            CreateKeyedSchema(ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes),
            KeyedWrapperQuery);

        Assert.Equal(
            ["Common", "OnlyA"],
            GetFragmentTypeNamesForField(sourceLocalPlan, "actions"));
        Assert.Equal(
            ["Common"],
            GetFragmentTypeNamesForField(commonRuntimeTypesPlan, "actions"));
    }

    [Fact]
    public void Plan_Should_NarrowProviderScopeAcrossProviderSpecificField()
    {
        var plan = PlanOperation(
            CreateKeyedSchema(ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes),
            """
            {
              rootA {
                bWrapper {
                  actions {
                    ... on Common { label }
                    ... on OnlyA { a }
                    ... on OnlyB { b }
                  }
                }
              }
            }
            """);

        Assert.Equal(
            ["Common", "OnlyB"],
            GetFragmentTypeNamesForField(plan, "actions"));
    }

    private static FusionSchemaDefinition CreateUnionSchema(
        string bConnectorKind,
        string? bFieldArguments)
        => CreateSchema(
            $$"""
            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              actions: [Action!]!
                @fusion__field(schema: A)
                @fusion__field(schema: B{{bFieldArguments}})
            }

            union Action
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__unionMember(schema: A, member: "Common")
              @fusion__unionMember(schema: A, member: "OnlyA")
              @fusion__unionMember(schema: B, member: "Common")
              @fusion__unionMember(schema: B, member: "OnlyB")
              = Common | OnlyA | OnlyB

            type Common @fusion__type(schema: A) @fusion__type(schema: B) {
              label: String @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type OnlyA @fusion__type(schema: A) {
              a: String @fusion__field(schema: A)
            }

            type OnlyB @fusion__type(schema: B) {
              b: String @fusion__field(schema: B)
            }
            """,
            bConnectorKind);

    private static FusionSchemaDefinition CreateKeyedSchema(
        ShareableFieldRuntimeTypeRouting routing)
        => CreateSchema(
            """
            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              rootA: Container @fusion__field(schema: A)
            }

            type Container
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__lookup(
                schema: B
                key: "id"
                field: "containerById(id: ID!): Container"
                map: ["id"]
                internal: true) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              wrapper: Wrapper @fusion__field(schema: A) @fusion__field(schema: B)
              bWrapper: Wrapper @fusion__field(schema: B)
            }

            type Wrapper @fusion__type(schema: A) @fusion__type(schema: B) {
              actions: [Action!]!
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }

            union Action
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__unionMember(schema: A, member: "Common")
              @fusion__unionMember(schema: A, member: "OnlyA")
              @fusion__unionMember(schema: B, member: "Common")
              @fusion__unionMember(schema: B, member: "OnlyB")
              = Common | OnlyA | OnlyB

            type Common @fusion__type(schema: A) @fusion__type(schema: B) {
              label: String @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type OnlyA @fusion__type(schema: A) {
              a: String @fusion__field(schema: A)
            }

            type OnlyB @fusion__type(schema: B) {
              b: String @fusion__field(schema: B)
            }
            """,
            routing: routing);

    private static FusionSchemaDefinition CreateSchema(
        string types,
        string bConnectorKind = "ApolloFederation",
        ShareableFieldRuntimeTypeRouting routing =
            ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes)
    {
        var document = Utf8GraphQLParser.Parse(
            $$"""
            schema
              @fusion__execution(
                shareableFieldRuntimeTypeRouting: {{routing switch
                {
                    ShareableFieldRuntimeTypeRouting.SourceLocal => "SOURCE_LOCAL",
                    ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes => "COMMON_RUNTIME_TYPES",
                    _ => throw new ArgumentOutOfRangeException(nameof(routing))
                }}}) {
              query: Query
            }

            {{types}}

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "a", kind: "ApolloFederation")
              B @fusion__schema_metadata(name: "b", kind: "{{bConnectorKind}}")
            }
            """);

        return FusionSchemaDefinition.Create(document);
    }

    private static string[] GetRootFragmentTypeNames(
        OperationPlan plan,
        params string[] fieldPath)
    {
        var root = Assert.IsType<OperationExecutionNode>(Assert.Single(plan.RootNodes));
        var document = Utf8GraphQLParser.Parse(root.Operation.SourceText);
        var operation = Assert.Single(document.Definitions.OfType<OperationDefinitionNode>());
        var selectionSet = operation.SelectionSet;

        foreach (var fieldName in fieldPath)
        {
            var field = Assert.Single(
                selectionSet.Selections.OfType<FieldNode>(),
                selection => selection.Name.Value == fieldName);
            selectionSet = Assert.IsType<SelectionSetNode>(field.SelectionSet);
        }

        return selectionSet.Selections
            .OfType<InlineFragmentNode>()
            .SelectMany(GetTypeNames)
            .ToArray();

        static IEnumerable<string> GetTypeNames(InlineFragmentNode fragment)
        {
            if (fragment.TypeCondition is { } typeCondition)
            {
                yield return typeCondition.Name.Value;
            }

            foreach (var child in fragment.SelectionSet.Selections.OfType<InlineFragmentNode>())
            {
                foreach (var typeName in GetTypeNames(child))
                {
                    yield return typeName;
                }
            }
        }
    }

    private static string[] GetFragmentTypeNamesForField(OperationPlan plan, string fieldName)
    {
        var typeNames = new List<string>();

        foreach (var node in plan.AllNodes)
        {
            var sourceText = node switch
            {
                OperationExecutionNode operationNode => operationNode.Operation.SourceText,
                ApolloOperationExecutionNode operationNode => operationNode.Operation.SourceText,
                _ => null
            };

            if (sourceText is null)
            {
                continue;
            }

            var document = Utf8GraphQLParser.Parse(sourceText);
            var operation = Assert.Single(document.Definitions.OfType<OperationDefinitionNode>());
            Collect(operation.SelectionSet);
        }

        return typeNames.ToArray();

        void Collect(SelectionSetNode selectionSet)
        {
            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode { SelectionSet: { } child } field:
                        if (field.Name.Value == fieldName)
                        {
                            foreach (var fragment in child.Selections.OfType<InlineFragmentNode>())
                            {
                                typeNames.AddRange(GetTypeNames(fragment));
                            }
                        }

                        Collect(child);
                        break;

                    case InlineFragmentNode fragment:
                        Collect(fragment.SelectionSet);
                        break;
                }
            }
        }

        static IEnumerable<string> GetTypeNames(InlineFragmentNode fragment)
        {
            if (fragment.TypeCondition is { } typeCondition)
            {
                yield return typeCondition.Name.Value;
            }

            foreach (var child in fragment.SelectionSet.Selections.OfType<InlineFragmentNode>())
            {
                foreach (var typeName in GetTypeNames(child))
                {
                    yield return typeName;
                }
            }
        }
    }

    private const string UnionQuery =
        "{ actions { ... on Common { label } ... on OnlyA { a } ... on OnlyB { b } } }";

    private const string KeyedWrapperQuery =
        "{ rootA { wrapper { actions { ... on Common { label } ... on OnlyA { a } ... on OnlyB { b } } } } }";
}
