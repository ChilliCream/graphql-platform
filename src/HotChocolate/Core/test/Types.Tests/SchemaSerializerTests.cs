using HotChocolate.Types;

namespace HotChocolate;

public class SchemaSerializerTests
{
    [Fact]
    public void SerializeSchemaWithDirective()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(FileResource.Open("serialize_schema.graphql"))
            .AddDirectiveType(new DirectiveType(t => t
                .Name("upper")
                .Location(DirectiveLocation.FieldDefinition)))
            .Use(next => next)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // act
        var serializedSchema = schema.ToString();

        // assert
        serializedSchema.MatchSnapshot();
    }

    [Fact]
    public void SerializeSchemaWithMutationWithoutSubscription()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(FileResource.Open("serialize_schema_with_mutation.graphql"))
            .Use(next => next)
            .Create();

        // act
        var serializedSchema = schema.ToString();

        // assert
        serializedSchema.MatchSnapshot();
    }

    public class Query
    {
        public required string Bar { get; set; }
    }
}
