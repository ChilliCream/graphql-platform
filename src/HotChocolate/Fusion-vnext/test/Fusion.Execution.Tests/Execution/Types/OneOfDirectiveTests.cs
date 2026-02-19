using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Types;

public class OneOfDirectiveTests : FusionTestBase
{
    [Fact]
    public void CreateSchema_With_Explicit_OneOf_Directive_Does_Not_Duplicate_Spec_Directives()
    {
        // arrange
        var compositeSchemaDoc = ComposeSchemaDocument(
            """
            type Query {
              foo(input: FooInput!): String!
            }

            input FooInput @oneOf {
              a: String
              b: String
            }
            """);

        var schemaDocumentWithOneOfDirective = Utf8GraphQLParser.Parse(
            $$"""
              {{compositeSchemaDoc}}

              directive @oneOf on INPUT_OBJECT
              """);

        // act
        var schema = FusionSchemaDefinition.Create(schemaDocumentWithOneOfDirective);

        // assert
        Assert.Single(
            schema.DirectiveDefinitions.AsEnumerable(),
            t => t.Name.Equals(DirectiveNames.OneOf.Name, StringComparison.Ordinal));

        var inputType = schema.Types.GetType<FusionInputObjectTypeDefinition>("FooInput");
        Assert.True(inputType.IsOneOf);
    }
}
