using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HotChocolate.Configuration.Validation;

public abstract class TypeValidationTestBase
{
    public static void ExpectValid(string schema)
    {
        SchemaBuilder.New()
            .AddDocumentFromString(schema)
            .Use(_ => _ => default)
            .Create();
    }

    public static void ExpectError([StringSyntax("graphql")] string schema, params Action<ISchemaError>[] errorAssert)
    {
        try
        {
            SchemaBuilder.New()
                .AddDocumentFromString(schema)
                .Use(_ => _ => default)
                .Create();
            Assert.Fail("Expected error!");
        }
        catch (SchemaException ex)
        {
            Assert.NotEmpty(ex.Errors);

            if (errorAssert.Length > 0)
            {
                Assert.Collection(ex.Errors, errorAssert);
            }

            var text = new StringBuilder();

            foreach (var error in ex.Errors)
            {
                text.AppendLine(error.ToString());
                text.AppendLine();
            }

            text.ToString().MatchSnapshot();
        }
    }
}
