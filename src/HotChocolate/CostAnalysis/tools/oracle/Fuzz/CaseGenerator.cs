using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis.Fuzz;

internal static class CaseGenerator
{
    private const string DirectiveDefinitions = """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(
          assumedSize: Int
          slicingArguments: [String!]
          slicingArgumentDefaultValue: Int
          sizedFields: [String!]
          requireOneSlicingArgument: Boolean = true
        ) on FIELD_DEFINITION
        directive @approx(tolerance: Float! @cost(weight: "-1")) on FIELD

        """;

    public static async Task<IReadOnlyList<FuzzCase>> GenerateAsync(int seed, int count)
    {
        var random = new Random(seed);
        var realSchema = await GeneratedSchema.CreateAsync();
        var cases = new List<FuzzCase>(count);

        for (var index = 0; index < count; index++)
        {
            var generated = index % 2 == 0
                ? CreateReal(seed, index, random, realSchema)
                : CreateSynthetic(seed, index, random);
            AssertAgreementScope(generated.Operation);
            AssertAgreementScope(generated.OracleOperation);
            cases.Add(WithExpected(generated));
        }

        return cases;
    }

    private static FuzzCase CreateReal(int seed, int index, Random random, string sdl)
    {
        var size = random.Next(0, 12);
        var condition = random.Next(0, 2) == 0;
        var defaultListSize = random.Next(1, 13);
        var variant = (index / 2) % 5;
        var operation = variant switch
        {
            0 => """
                query Case($n: Int!, $show: Boolean!) {
                  books(first: $n, where: { title: { contains: "a" } }, order: [{ pages: DESC }]) {
                    nodes { title pages @include(if: $show) }
                  }
                }
                """,
            1 => """
                query Case($show: Boolean!) {
                  books(where: { pages: { gt: 10 } }) {
                    nodes { title @include(if: $show) pages @skip(if: $show) }
                  }
                }
                """,
            2 => """
                query Case($n: Int!, $show: Boolean!) {
                  booksOffset(skip: 1, take: $n, order: [{ title: ASC }]) {
                    items { title pages @include(if: $show) }
                  }
                }
                """,
            3 => """
                query Case($show: Boolean!) {
                  booksOffset(where: { title: { startsWith: "B" } }) {
                    items { title @skip(if: $show) pages }
                  }
                }
                """,
            _ => """
                query Case($show: Boolean!) {
                  labels @include(if: $show)
                  same: labels
                  same: labels
                }
                """
        };
        var oracleOperation = variant switch
        {
            1 => operation.Replace("books(where:", "books(first: 7, where:", StringComparison.Ordinal),
            3 => operation.Replace("booksOffset(where:", "booksOffset(take: 9, where:", StringComparison.Ordinal),
            _ => operation
        };

        return NewCase(
            index,
            seed,
            "interceptor",
            sdl,
            operation,
            oracleOperation,
            new { n = size, show = condition },
            defaultListSize);
    }

    private static FuzzCase CreateSynthetic(int seed, int index, Random random)
    {
        var aWeight = random.Next(-3, 10);
        var bWeight = random.Next(-3, 10);
        var leafWeight = random.Next(-4, 12);
        var assumedSize = random.Next(1, 10);
        var defaultSlice = random.Next(1, 10);
        var defaultListSize = random.Next(1, 13);
        var sdl = DirectiveDefinitions + $$"""
            interface Node { id: ID! name: String }
            type A implements Node @cost(weight: "{{aWeight}}") {
              id: ID!
              name: String @cost(weight: "{{leafWeight}}")
              value: Int @cost(weight: "3")
            }
            type B implements Node @cost(weight: "{{bWeight}}") {
              id: ID!
              name: String @cost(weight: "{{leafWeight + 1}}")
              value: Int @cost(weight: "5")
            }
            union SearchResult = A | B
            input RangeInput { low: Int = 1 high: Int @cost(weight: "2") }
            input SearchInput { term: String! @cost(weight: "-1") range: RangeInput = { high: 4 } }
            type Box { items: [A] @cost(weight: "1") }
            type Query {
              nodes(limit: Int, filter: SearchInput @cost(weight: "2")): [Node]
                @cost(weight: "1")
                @listSize(assumedSize: {{assumedSize}}, slicingArguments: ["limit"])
              results(limit: Int): [SearchResult]
                @cost(weight: "2")
                @listSize(slicingArguments: ["limit"], assumedSize: {{assumedSize}})
              boxes(limit: Int): [Box]
                @listSize(slicingArguments: ["limit"], sizedFields: ["items"], assumedSize: {{assumedSize}})
              defaulted(first: Int): [A]
                @listSize(slicingArguments: ["first"], slicingArgumentDefaultValue: {{defaultSlice}})
              decorated: String @cost(weight: "4")
            }
            """;
        var size = random.Next(0, 12);
        var condition = random.Next(0, 2) == 0;
        var variant = (index / 2) % 5;
        var operation = variant switch
        {
            0 => """
                query Case($n: Int!, $show: Boolean!) {
                  nodes(limit: $n, filter: { term: "x", range: { high: 3 } }) {
                    ...NodeFields
                    ... on A { value @include(if: $show) }
                    ... on B { value @skip(if: $show) }
                  }
                }
                fragment NodeFields on Node { id alias: name }
                """,
            1 => """
                query Case($n: Int!, $show: Boolean!) {
                  results(limit: $n) {
                    ... on A { same: value @include(if: $show) }
                    ... on A { same: value @include(if: $show) }
                    ... on B { name @skip(if: $show) }
                  }
                }
                """,
            2 => """
                query Case($n: Int!) {
                  boxes(limit: $n) { items { name value } }
                }
                """,
            3 => """
                query Case { defaulted { name value } }
                """,
            _ => """
                query Case($show: Boolean!) {
                  decorated @approx(tolerance: 0.5) @include(if: $show)
                }
                """
        };
        var oracleOperation = variant == 3
            ? operation.Replace("defaulted {", $"defaulted(first: {defaultSlice}) {{", StringComparison.Ordinal)
            : operation;

        return NewCase(
            index,
            seed,
            "synthetic",
            sdl,
            operation,
            oracleOperation,
            new { n = size, show = condition },
            defaultListSize);
    }

    private static FuzzCase NewCase(
        int index,
        int seed,
        string source,
        string sdl,
        string operation,
        string oracleOperation,
        object variables,
        int defaultListSize)
        => new(
            $"case-{index:D5}-{source}",
            seed,
            source,
            sdl,
            operation,
            oracleOperation,
            "Case",
            JsonSerializer.SerializeToElement(variables),
            defaultListSize,
            FuzzCost.From(0, 0));

    private static FuzzCase WithExpected(FuzzCase generated)
    {
        var schema = SchemaParser.Parse(generated.Sdl);
        var document = Utf8GraphQLParser.Parse(generated.Operation);
        var operation = document.Definitions
            .OfType<OperationDefinitionNode>()
            .Single(definition => definition.Name?.Value == generated.OperationName);
        var snapshot = CostSchemaSnapshot.Create(
            schema,
            new CostEngineOptions { DefaultListSize = generated.DefaultListSize });
        var plan = CostPlanCompiler.Compile(snapshot, document, operation, CostAnalyses.Cost);
        var estimate = plan.Evaluate(new JsonCostVariableValues(generated.Variables));

        return generated with { Expected = FuzzCost.From(estimate.TypeCost, estimate.FieldCost) };
    }

    private static void AssertAgreementScope(string operation)
    {
        if (operation.Contains("__typename", StringComparison.Ordinal)
            || operation.Contains("__schema", StringComparison.Ordinal)
            || operation.Contains("__type", StringComparison.Ordinal)
            || operation.Contains("@defer", StringComparison.Ordinal)
            || operation.Contains("@stream", StringComparison.Ordinal))
        {
            throw ThrowHelper.InvalidOperation(
                "Generated operations must exclude introspection, @defer, and @stream.");
        }

        var document = Utf8GraphQLParser.Parse(operation);
        if (document.Definitions.OfType<OperationDefinitionNode>().Any(t => t.Operation != OperationType.Query))
        {
            throw ThrowHelper.InvalidOperation(
                "The differential agreement scope is query operations only.");
        }
    }
}
