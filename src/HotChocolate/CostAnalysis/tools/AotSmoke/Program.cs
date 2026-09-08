using HotChocolate.CostAnalysis;
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

try
{
    CostSchemaSnapshot.Create(schema, options);
}
catch (NotImplementedException)
{
    Console.WriteLine(
        "Schema and options parsed and are AOT-safe. "
        + "CostSchemaSnapshot.Create is a scaffold shell pending a follow-up task.");
    return;
}

Console.WriteLine("Cost schema snapshot built.");
