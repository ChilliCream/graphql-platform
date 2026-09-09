using HotChocolate.CostAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

const string sourceText =
    """
    directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
    directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION

    union Result = A | B

    type A {
      a: Int @cost(weight: "10")
    }

    type B {
      b: Int @cost(weight: "20")
    }

    type Query {
      result: Result
    }
    """;

var schema = SchemaParser.Parse(sourceText);
var options = new CostEngineOptions();
var snapshot = CostSchemaSnapshot.Create(schema, options);
var document = Utf8GraphQLParser.Parse("{ result { ... on A { a } ... on B { b } } }");
var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
var plan = CostPlanCompiler.Compile(snapshot, document, operation, CostAnalyses.Cost);
var estimate = plan.EvaluateStaticBound();

if (estimate != new CostEstimate(21.0, 2.0, null))
{
    Console.Error.WriteLine($"AOT smoke expected 2/21, got {estimate.TypeCost}/{estimate.FieldCost}.");
    Environment.ExitCode = 1;
    return;
}

Console.WriteLine("Cost plan compiled and evaluated to 2/21.");
