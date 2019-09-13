using System;

namespace HotChocolate.Types
{
    public class TypeTestBase
    {
        protected T CreateDirective<T>(T directiveType)
            where T : DirectiveType
        {
            return CreateDirective(directiveType, b => { });
        }

        protected T CreateDirective<T>(T directiveType,
            Action<ISchemaBuilder> configure)
            where T : DirectiveType
        {
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar"))
                .AddDirectiveType(directiveType);

            configure(builder);

            builder.Create();

            return directiveType;
        }

        protected static T CreateType<T>(T type)
            where T : INamedType
        {
            return CreateType(type, b => { });
        }

        protected static T CreateType<T>(T type,
            Action<ISchemaBuilder> configure)
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

        public static ISchema CreateSchema<T>(T type)
            where T : INamedType =>
            CreateSchema(builder => builder.AddType(type));

        public static ISchema CreateSchema(Action<ISchemaBuilder> configure)
        {
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar"));

            configure(builder);

            return builder.Create();
        }
    }
}
