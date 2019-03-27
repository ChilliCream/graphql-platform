namespace HotChocolate.Types
{
    public class TypeTestBase
    {
        public static T CreateType<T>(T type)
            where T : INamedType
        {
            return CreateType(type, b => { });
        }

        public static T CreateType<T>(T type, Action<ISchemaBuilder> configure)
            where T : INamedType
        {
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar"))
                .AddType(type);

            configure(builder);

            builder.Create();

            return type;
        }
    }
}