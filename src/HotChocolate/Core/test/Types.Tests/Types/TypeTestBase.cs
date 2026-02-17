namespace HotChocolate.Types;

public class TypeTestBase
{
    protected T CreateDirective<T>(T directiveType)
        where T : DirectiveType
        => CreateDirective(directiveType, b => { });

    protected T CreateDirective<T>(T directiveType,
        Action<ISchemaBuilder> configure)
        where T : DirectiveType
    {
        var builder = SchemaBuilder.New()
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddDirectiveType(directiveType);

        configure(builder);

        builder.Create();

        return directiveType;
    }

    protected static T CreateType<T>(T type)
        where T : ITypeDefinition
        => CreateType(type, b => { });

    protected static T CreateType<T>(T type,
        Action<ISchemaBuilder> configure)
        where T : ITypeDefinition
    {
        var builder = SchemaBuilder.New()
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddType(type);

        configure(builder);

        builder.Create();

        return type;
    }

    public static Schema CreateSchema<T>(T type)
        where T : ITypeDefinition =>
        CreateSchema(builder => builder.AddType(type));

    public static Schema CreateSchema(Action<ISchemaBuilder> configure)
    {
        var builder = SchemaBuilder.New()
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"));

        configure(builder);

        return builder.Create();
    }
}
