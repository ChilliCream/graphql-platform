using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public sealed class StaticBoundSoundnessTests
{
    [Fact]
    public void EvaluateStaticBound_Should_DominateEvaluation_When_VariablesStayWithinAssumptions()
    {
        // arrange
        var random = new Random(1729);
        var failures = new List<string>();

        // act
        for (var index = 0; index < 200; index++)
        {
            var includeFirst = index % 2 == 0;
            var operationSource = includeFirst
                ? "query($n:Int!,$x:Boolean!,$input:Filter){ items(limit:$n,filter:$input){ value @include(if:$x) detail @skip(if:$x) } }"
                : "query($n:Int!,$x:Boolean!,$input:Filter){ items(limit:$n,filter:$input){ detail @include(if:$x) value @skip(if:$x) } }";
            var schema = SchemaParser.Parse(SchemaSource);
            var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
            var document = Utf8GraphQLParser.Parse(operationSource);
            var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
            var plan = CostPlanCompiler.Compile(
                snapshot,
                document,
                operation,
                CostAnalyses.Cost | CostAnalyses.ResponseSize);
            var values = new SoundnessVariableValues(
                random.Next(0, 11),
                random.Next(0, 2) == 1,
                random.Next(0, 2) == 1);
            var bound = plan.EvaluateStaticBound();
            var evaluated = plan.Evaluate(values);

            if (bound.TypeCost < evaluated.TypeCost
                || bound.FieldCost < evaluated.FieldCost
                || bound.MaxResponseSize < evaluated.MaxResponseSize)
            {
                failures.Add($"case {index}: bound {bound}, evaluated {evaluated}");
            }
        }

        // assert
        Assert.Empty(failures);
    }

    private const string SchemaSource =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
        input Filter { terms: [Term] @cost(weight: "2") }
        input Term { value: String @cost(weight: "3") }
        type Item { value: Int @cost(weight: "2") detail: Int @cost(weight: "4") }
        type Query {
          items(limit: Int!, filter: Filter @cost(weight: "5")): [Item]
            @listSize(assumedSize: 10, slicingArguments: ["limit"])
        }
        """;

    private sealed class SoundnessVariableValues(
        int size,
        bool condition,
        bool includeInput) : ICostVariableValues
    {
        public bool TryGetValue(string name, out IValueNode? value)
        {
            value = name switch
            {
                "n" => new IntValueNode(size),
                "x" => new BooleanValueNode(condition),
                "input" when includeInput => new ObjectValueNode(
                    new ObjectFieldNode(
                        "terms",
                        new ListValueNode(
                            new ObjectValueNode(new ObjectFieldNode("value", "x"))))),
                _ => null
            };
            return value is not null;
        }
    }
}
