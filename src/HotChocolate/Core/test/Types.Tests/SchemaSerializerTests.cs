using System.Text;
using HotChocolate.Types;

namespace HotChocolate;

public class SchemaSerializerTests
{
    [Fact]
    public void Serialize_SchemaIsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action() => SchemaPrinter.Print(null);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void SerializeSchemaWriter_SchemaIsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action() => SchemaPrinter.Serialize(null, new StringWriter());

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void SerializeSchemaWriter_WriterIsNull_ArgumentNullException()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo: String }")
            .AddResolver("Query", "foo", "bar")
            .Create();

        // act
        void Action() => SchemaPrinter.Serialize(schema, null);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public async Task SerializeAsync_SchemaIsNull_ArgumentNullException()
    {
        // arrange
        // act
        async Task Action() => await SchemaPrinter.PrintAsync(
            default(ISchema),
            new MemoryStream());

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }

    [Fact]
    public async Task SerializeAsync_WriterIsNull_ArgumentNullException()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo: String }")
            .AddResolver("Query", "foo", "bar")
            .Create();

        // act
        async Task Action() => await SchemaPrinter.PrintAsync(schema, null);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }

    [Fact]
    public void SerializeSchemaWriter_Serialize()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo: String }")
            .AddResolver("Query", "foo", "bar")
            .Create();
        var stringBuilder = new StringBuilder();

        // act
        SchemaPrinter.Serialize(schema, new StringWriter(stringBuilder));

        // assert
        stringBuilder.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task SerializeAsync_Serialize()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo: String }")
            .AddResolver("Query", "foo", "bar")
            .Create();
        using var stream = new MemoryStream();

        // act
        await SchemaPrinter.PrintAsync(schema, stream);

        // assert
        Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
    }

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

    [Fact]
    public async Task SerializeTypes()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(FileResource.Open("serialize_schema_with_mutation.graphql"))
            .Use(next => next)
            .Create();

        // act
        using var stream = new MemoryStream();
        await SchemaPrinter.PrintAsync(
            new INamedType[] { schema.QueryType, },
            stream,
            true);

        // assert
        Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
    }

    [Fact]
    public async Task SerializeTypes_Types_Is_Null()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(FileResource.Open("serialize_schema_with_mutation.graphql"))
            .Use(next => next)
            .Create();

        // act
        using var stream = new MemoryStream();
        async Task Fail() => await SchemaPrinter.PrintAsync(
            default(IEnumerable<INamedType>),
            stream,
            true);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task SerializeTypes_Stream_Is_Null()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(FileResource.Open("serialize_schema_with_mutation.graphql"))
            .Use(next => next)
            .Create();

        // act
        using var stream = new MemoryStream();
        async Task Fail() => await SchemaPrinter.PrintAsync(
            new INamedType[] { schema.QueryType, },
            null,
            true);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Fail);
    }

    public class Query
    {
        public string Bar { get; set; }
    }
}
