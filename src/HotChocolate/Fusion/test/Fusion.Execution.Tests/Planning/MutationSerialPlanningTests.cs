using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class MutationSerialPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_KeepEachEntityLookupWithItsRoot_When_ApolloMutationRootsSelectSameEntityField()
    {
        // arrange
        var schema = CreateApolloLedgerSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation Serial {
              a: bump(counter: "serial", by: 1) {
                value
              }
              b: bump(counter: "serial", by: 10) {
                value
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepEachEntityLookupWithItsRoot_When_ApolloLookupsAreGroupedWithoutMerging()
    {
        // arrange
        var schema = CreateApolloLedgerSchema();
        var options = new OperationPlannerOptions { MergePolicy = OperationMergePolicy.None };

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation Serial {
              a: bump(counter: "serial", by: 1) {
                value
              }
              b: bump(counter: "serial", by: 10) {
                value
              }
            }
            """,
            options);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepEachLookupWithItsRoot_When_CompositeMutationRootsSelectSameEntityField()
    {
        // arrange
        var schema = CreateCompositeLedgerSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation Serial {
              a: bump(counter: "serial", by: 1) {
                value
              }
              b: bump(counter: "serial", by: 10) {
                value
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepEachLookupWithItsRoot_When_CompositeLookupsAreGroupedWithoutMerging()
    {
        // arrange
        var schema = CreateCompositeLedgerSchema();
        var options = new OperationPlannerOptions { MergePolicy = OperationMergePolicy.None };

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation Serial {
              a: bump(counter: "serial", by: 1) {
                value
              }
              b: bump(counter: "serial", by: 10) {
                value
              }
            }
            """,
            options);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_OrderRootsByFirstIncludedOccurrence_When_ResponseNameRepeatsUnderConditions()
    {
        // arrange
        var schema = CreateApolloCatalogSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation Conditioned($counter: ID!, $a: Boolean!, $b: Boolean!) {
              first: bump(counter: $counter, by: 1) @include(if: $a)
              second: add(counter: $counter, by: 10) @include(if: $b)
              ...Again @include(if: $b)
              last: bump(counter: $counter, by: 100)
            }

            fragment Again on Mutation {
              first: bump(counter: $counter, by: 1)
            }
            """);

        // assert
        MatchPlanAndRootOrder(plan, "a", "b");
    }

    [Fact]
    public void Plan_Should_RunMergedRootOnceWithItsLookup_When_IncludedOccurrencePrecedesUnconditionalOne()
    {
        // arrange
        var schema = CreateApolloLedgerSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation Included($x: Boolean!) {
              a: bump(counter: "included", by: 1) @include(if: $x) {
                value
              }
              b: bump(counter: "included", by: 10) {
                value
              }
              a: bump(counter: "included", by: 1) {
                value
              }
            }
            """);

        // assert
        MatchPlanAndRootOrder(plan, "x");
    }

    [Fact]
    public void Plan_Should_PlanOneDeliveryGroupPerLabel_When_LaterOccurrenceDefersUnderConditionedOne()
    {
        // arrange
        var schema = CreateCompositeLedgerSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation M($x: Boolean!) {
              a: bump(counter: "s", by: 1) @include(if: $x) {
                id
              }
              a: bump(counter: "s", by: 1) {
                ... @defer(label: "d") {
                  value
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_PlanOneDeliveryGroupPerLabel_When_EarlierOccurrenceHasTwoConditions()
    {
        // arrange
        var schema = CreateCompositeLedgerSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation M($x: Boolean!, $y: Boolean!) {
              ... @include(if: $x) {
                a: bump(counter: "s", by: 1) @include(if: $y) {
                  id
                }
              }
              a: bump(counter: "s", by: 1) {
                ... @defer(label: "d") {
                  value
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static void MatchPlanAndRootOrder(OperationPlan plan, params string[] variableNames)
    {
        var snapshot = Snapshot.Create().Add(
            new YamlOperationPlanFormatter().Format(plan),
            "Plan",
            MarkdownLanguages.Yaml);

        for (var valuation = (1 << variableNames.Length) - 1; valuation >= 0; valuation--)
        {
            var variables = new Dictionary<string, bool>();

            for (var i = 0; i < variableNames.Length; i++)
            {
                variables[variableNames[i]] = (valuation & (1 << (variableNames.Length - 1 - i))) != 0;
            }

            var rootFields = plan.RootNodes
                .Where(node => IsIncluded(node, variables))
                .Select(node => $"{GetRootResponseName(node)} ({node.Id})");

            snapshot.Add(
                string.Join(", ", rootFields),
                "Root order when " + string.Join(", ", variables.Select(t => $"${t.Key}: {(t.Value ? "true" : "false")}")),
                MarkdownLanguages.Text);
        }

        snapshot.MatchMarkdownSnapshot();

        static bool IsIncluded(ExecutionNode node, Dictionary<string, bool> variables)
        {
            foreach (var condition in node.Conditions)
            {
                if (variables[condition.VariableName] != condition.PassingValue)
                {
                    return false;
                }
            }

            return true;
        }

        static string GetRootResponseName(ExecutionNode node)
        {
            var operation = (OperationExecutionNode)node;
            var document = Utf8GraphQLParser.Parse(operation.Operation.Value.Span);
            var selectionSet = document.Definitions.OfType<OperationDefinitionNode>().Single().SelectionSet;

            while (selectionSet.Selections[0] is InlineFragmentNode inlineFragment)
            {
                selectionSet = inlineFragment.SelectionSet;
            }

            var field = (FieldNode)selectionSet.Selections[0];
            return field.Alias?.Value ?? field.Name.Value;
        }
    }

    private static FusionSchemaDefinition CreateApolloLedgerSchema()
    {
        return ComposeSchema(
            """
            # name: ledger
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
              ping: String
            }

            type Mutation {
              bump(counter: ID!, by: Int!): Counter
            }

            type Counter @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: reader
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Counter @key(fields: "id") {
              id: ID!
              value: Int
            }
            """);
    }

    private static FusionSchemaDefinition CreateCompositeLedgerSchema()
    {
        return ComposeSchema(
            """
            # name: ledger
            type Query {
              ping: String
            }

            type Mutation {
              bump(counter: ID!, by: Int!): Counter
            }

            type Counter @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: reader
            type Query {
              counterById(id: ID!): Counter @lookup
            }

            type Counter @key(fields: "id") {
              id: ID!
              value: Int
            }
            """);
    }

    private static FusionSchemaDefinition CreateApolloCatalogSchema()
    {
        return ComposeSchema(
            """
            # name: catalog
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
              items: [Item]
            }

            type Mutation {
              bump(counter: ID!, by: Int!): Int
            }

            type Item @key(fields: "id") {
              id: ID!
              name: String
            }
            """,
            """
            # name: details
            extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Mutation {
              add(counter: ID!, by: Int!): Int
            }

            type Item @key(fields: "id") {
              id: ID!
              detail: Detail
            }

            type Detail {
              summary: String
              rating: Int
            }
            """);
    }
}
