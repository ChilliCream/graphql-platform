using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public sealed class StaticBoundSoundnessTests
{
    private const int SampleCount = 200;
    private const int BaseSeed = 1729;

    [Fact]
    public void EvaluateStaticBound_Should_DominateEvaluation_When_VariedShapesStayWithinAssumptions()
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
            var snapshot = CostSchemaSnapshot.Create(
                schema,
                new CostEngineOptions { DefaultListSize = random.Next(1, 7) });
            var plan = CostPlanCompiler.Compile(
                snapshot,
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
                    out var coerced,
                    out var error))
            {
                failures.Add(
                    $"seed {seed}: coercion failed: {error.Message}\noperation: {operationSource}\nvariables: {rawVariables}");
                continue;
            }

            var bound = plan.EvaluateStaticBound();
            var evaluated = plan.Evaluate(new VariableValuesAdapter(coerced));

            if (bound.TypeCost < evaluated.TypeCost
                || bound.FieldCost < evaluated.FieldCost
                || bound.MaxResponseSize < evaluated.MaxResponseSize)
            {
                failures.Add(
                    $"seed {seed}: bound {bound}, evaluated {evaluated}\noperation: {operationSource}\nvariables: {rawVariables}");
            }
        }

        // assert
        Assert.Empty(failures);
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
