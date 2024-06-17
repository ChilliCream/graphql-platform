namespace HotChocolate.Skimmed;

public class SchemaTests
{
    [Fact]
    public void SealSchema()
    {
        var schema = new SchemaDefinition();

        var stringType = BuiltIns.String.Create();
        schema.Types.Add(stringType);

        var queryType = new ObjectTypeDefinition("Query");
        queryType.Fields.Add(new("foo", stringType));
        schema.QueryType = queryType;

        schema.Seal();

        Assert.Throws<NotSupportedException>(NotSupported);
        return;

        void NotSupported() => queryType.Fields.Add(new OutputFieldDefinition("baz", stringType));
    }

    [Fact]
    public void SealSchemaWithDirective()
    {
        var schema = new SchemaDefinition();

        var stringType = BuiltIns.String.Create();
        schema.Types.Add(stringType);

        var queryType = new ObjectTypeDefinition("Query");
        queryType.Fields.Add(new("foo", stringType));
        schema.QueryType = queryType;

        var directive = new DirectiveDefinition("foo");
        schema.DirectiveDefinitions.Add(directive);

        schema.Seal();

        Assert.Throws<NotSupportedException>(NotSupported);
        return;

        void NotSupported() => schema.DirectiveDefinitions.Add(new DirectiveDefinition("bar"));
    }
}
