using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public sealed class AssumedBoundTests
{
    private const int SampleCount = 200;
    private const int BaseSeed = 1729;

    [Fact]
    public void EvaluateAssumedBound_Should_DominateEvaluation_When_ValuesStayWithinAssumptions()
    {
        // arrange
        var schema = SchemaParser.Parse(SchemaSource);
        var coercionSchema = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(SchemaSource + "\nenum fusion__Schema { TEST }"));
        var failures = new List<string>();

        // act
        for (var sample = 0; sample < SampleCount; sample++)
        {
            var seed = BaseSeed + sample;
            var random = new Random(seed);
            var operationSource = GenerateOperation(random, sample, out var sizeVariables);
            var document = Utf8GraphQLParser.Parse(operationSource);
            var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
            var schemaIndex = CostSchemaIndex.Create(
                schema,
                new CostSchemaIndexOptions { DefaultListSize = random.Next(1, 7) });
            var plan = CostPlanCompiler.Compile(
                schemaIndex,
                document,
                operation,
                CostAnalyses.Cost | CostAnalyses.ResponseSize);
            var rawVariables = CreateRawVariables(random, sizeVariables);

            using var variablesDocument = JsonDocument.Parse(rawVariables);

            if (!VariableCoercionHelper.TryCoerceVariableValues(
                    new TestFeatureProvider(),
                    coercionSchema,
                    operation.VariableDefinitions,
                    variablesDocument.RootElement,
                    false,
                    out var coerced,
                    out var error))
            {
                failures.Add(
                    $"seed {seed}: coercion failed: {error.Message}\noperation: {operationSource}"
                    + $"\nvariables: {rawVariables}");
                continue;
            }

            var bound = plan.EvaluateAssumedBound();
            var evaluated = plan.Evaluate(new VariableValuesAdapter(coerced));

            if (bound.TypeCost < evaluated.TypeCost
                || bound.FieldCost < evaluated.FieldCost
                || bound.MaxResponseSize < evaluated.MaxResponseSize)
            {
                failures.Add(
                    $"seed {seed}: bound {bound}, evaluated {evaluated}\noperation: {operationSource}"
                    + $"\nvariables: {rawVariables}");
            }
        }

        // assert
        Assert.Empty(failures);
    }

    [Fact]
    public void EvaluateAssumedBound_Should_BeExceeded_When_SlicingVariableExceedsAssumedSize()
    {
        // arrange: the assumed bound resolves a variable-bound slicing argument to
        // assumedSize (4); a runtime value above that is not covered by the bound.
        var plan = CompileSimplePlan(
            """
            type Item { value: Int @cost(weight: "2") }
            type Query {
              items(limit: Int!): [Item]
                @listSize(assumedSize: 4, slicingArguments: ["limit"])
            }
            """,
            "query($limit: Int!) { items(limit: $limit) { value } }");

        // act
        var assumed = plan.EvaluateAssumedBound();
        var evaluated = plan.Evaluate(SimpleVariables(("limit", new IntValueNode(20))));

        // assert
        Assert.True(
            evaluated.FieldCost > assumed.FieldCost,
            $"evaluated field cost {evaluated.FieldCost} was not above assumed bound {assumed.FieldCost}.");
        Assert.True(
            evaluated.TypeCost > assumed.TypeCost,
            $"evaluated type cost {evaluated.TypeCost} was not above assumed bound {assumed.TypeCost}.");
    }

    [Fact]
    public void EvaluateAssumedBound_Should_BeExceeded_When_VariableInputListSuppliesMoreThanOneElement()
    {
        // arrange: the assumed bound prices a variable-supplied input list as one
        // element (InputCost.ComputeStaticShape); three supplied elements are not.
        var plan = CompileSimplePlan(
            """
            input Term { value: String @cost(weight: "3") }
            type Query { search(terms: [Term] @cost(weight: "2")): Int }
            """,
            "query($t: [Term]) { search(terms: $t) }");
        var suppliedTerms = new ListValueNode(
            new ObjectValueNode(new ObjectFieldNode("value", "a")),
            new ObjectValueNode(new ObjectFieldNode("value", "b")),
            new ObjectValueNode(new ObjectFieldNode("value", "c")));

        // act
        var assumed = plan.EvaluateAssumedBound();
        var evaluated = plan.Evaluate(SimpleVariables(("t", suppliedTerms)));

        // assert
        Assert.True(
            evaluated.FieldCost > assumed.FieldCost,
            $"evaluated field cost {evaluated.FieldCost} was not above assumed bound {assumed.FieldCost}.");
    }

    private static CostPlan CompileSimplePlan(string typeSystemSource, string operationSource)
    {
        const string directives =
            """
            directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
            directive @listSize(assumedSize: Int, slicingArguments: [String!], slicingArgumentDefaultValue: Float, sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION

            """;
        var schema = SchemaParser.Parse(directives + typeSystemSource);
        var schemaIndex = CostSchemaIndex.Create(schema, new CostSchemaIndexOptions());
        var document = Utf8GraphQLParser.Parse(operationSource);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        return CostPlanCompiler.Compile(schemaIndex, document, operation, CostAnalyses.Cost);
    }

    private static ICostVariableValues SimpleVariables(params (string Name, IValueNode Value)[] values)
        => new SimpleVariableValues(values.ToDictionary(pair => pair.Name, pair => pair.Value));

    private sealed class SimpleVariableValues(
        IReadOnlyDictionary<string, IValueNode> values) : ICostVariableValues
    {
        public bool TryGetValue(string name, out IValueNode? value)
        {
            if (values.TryGetValue(name, out var found))
            {
                value = found;
                return true;
            }

            value = null;
            return false;
        }
    }

    private static string GenerateOperation(
        Random random,
        int sample,
        out string[] sizeVariables)
    {
        var rootCount = random.Next(1, 4);
        sizeVariables = new string[rootCount];
        var definitions = new StringBuilder();
        var selections = new StringBuilder();

        for (var root = 0; root < rootCount; root++)
        {
            if (root > 0)
            {
                definitions.Append(',');
            }

            var sizeVariable = $"n{root}";
            sizeVariables[root] = sizeVariable;
            definitions.Append('$').Append(sizeVariable).Append(":Int!");
            var rootField = random.Next(2) == 0 ? "items" : "featured";
            selections
                .Append(" r").Append(sample).Append('_').Append(root)
                .Append(':').Append(rootField)
                .Append("(limit:$").Append(sizeVariable).Append(",filter:$input){");
            AppendItemSelections(selections, random, sizeVariable, sample);
            selections.Append(" }");
        }

        definitions.Append(",$x:Boolean!,$y:Boolean!,$input:Filter");
        return new StringBuilder("query Case")
            .Append(sample)
            .Append('(').Append(definitions).Append(") {")
            .Append(selections)
            .Append(" }")
            .ToString();
    }

    private static void AppendItemSelections(
        StringBuilder selections,
        Random random,
        string sizeVariable,
        int sample)
    {
        if (random.Next(2) == 0)
        {
            selections.Append(" value @include(if:$x)");
        }
        else
        {
            selections.Append(" value @skip(if:$y)");
        }

        if (random.Next(2) == 0)
        {
            selections.Append(" detail { score @include(if:$y) tag @skip(if:$x) }");
        }
        else
        {
            selections.Append(" detail @include(if:$x) { tag score }");
        }

        if (random.Next(3) != 0)
        {
            selections
                .Append(" related(limit:$").Append(sizeVariable)
                .Append(") { score tag @include(if:$")
                .Append((sample & 1) == 0 ? 'x' : 'y')
                .Append(") }");
        }
    }

    private static string CreateRawVariables(Random random, string[] sizeVariables)
    {
        var variables = new Dictionary<string, object?>
        {
            ["x"] = random.Next(2) == 0,
            ["y"] = random.Next(2) == 0,
            ["input"] = random.Next(3) == 0
                ? null
                : new
                {
                    terms = random.Next(2) == 0
                        ? Array.Empty<object>()
                        : [new { value = $"term-{random.Next(100)}" }],
                    boost = random.Next(5)
                }
        };

        foreach (var variable in sizeVariables)
        {
            variables[variable] = random.Next(0, 5);
        }

        return JsonSerializer.Serialize(variables);
    }

    private const string SchemaSource =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], slicingArgumentDefaultValue: Float, sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
        input Filter { terms: [Term] @cost(weight: "2") boost: Int @cost(weight: "1") }
        input Term { value: String @cost(weight: "3") }
        type Detail { score: Int @cost(weight: "3") tag: String @cost(weight: "2") }
        type Item {
          value: Int @cost(weight: "2")
          detail: Detail @cost(weight: "1")
          related(limit: Int!): [Detail]
            @listSize(assumedSize: 4, slicingArguments: ["limit"])
        }
        type Query {
          items(limit: Int!, filter: Filter @cost(weight: "5")): [Item]
            @listSize(assumedSize: 10, slicingArguments: ["limit"])
          featured(limit: Int!, filter: Filter @cost(weight: "4")): [Item]
            @listSize(assumedSize: 6, slicingArguments: ["limit"])
        }
        """;

    private sealed class VariableValuesAdapter(
        IReadOnlyDictionary<string, VariableValue> values) : ICostVariableValues
    {
        public bool TryGetValue(string name, out IValueNode? value)
        {
            if (values.TryGetValue(name, out var variable))
            {
                value = variable.Value;
                return true;
            }

            value = null;
            return false;
        }
    }

    private sealed class TestFeatureProvider : IFeatureProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}
