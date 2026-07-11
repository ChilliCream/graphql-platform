using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class OperationNotSupportedFieldRuleTests()
    : DocumentValidatorVisitorTestBase(builder => builder.AddOperationRules())
{
    [Fact]
    public void Ensure_Non_Existent_Root_Types_Cause_Error()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            subscription {
                foo
            }
            """);
        var context = ValidationUtils.CreateContext(document, CreateQueryOnlySchema());

        // act
        Rule.Validate(context, document);

        // assert
        Assert.Collection(
            context.Errors,
            t => Assert.Equal(
                "This GraphQL schema does not support `Subscription` operations.",
                t.Message));
    }

    private static ISchemaDefinition CreateQueryOnlySchema()
    {
        return SchemaBuilder.New()
            .AddDocumentFromString(
                """
                type Query {
                    foo: String
                }
                """)
            .Use(next => next)
            .Create();
    }
}
