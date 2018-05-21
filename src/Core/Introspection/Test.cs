using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate.Introspection
{
    internal class Foo
        : SchemaConfiguration
    {
        public Foo()
        {
            RegisterObjectType(context => new ObjectTypeConfig
            {
                Name = "__Schema",
                Description =
                    "A GraphQL Schema defines the capabilities of a GraphQL server. It " +
                    "exposes all available types and directives on the server, as well as " +
                    "the entry points for query, mutation, and subscription operations.",
                IsIntrospection = true,
                Fields = new[]
                {
                    new Field(new FieldConfig
                    {
                        Name = "types",
                        Description = "A list of all types supported by this server.",
                        IsIntrospection = true,
                        Type = () => new NonNullType(new ListType(new NonNullType(context.GetOutputType("__Type")))),
                        Resolver = () => (ctx, ct) => ctx.Schema.GetAllTypes()
                    }),
                    new Field(new FieldConfig
                    {
                        Name = "",
                        Description = "",
                        IsIntrospection = true,
                        Resolver = () => (ctx, ct) => null
                    }),
                    new Field(new FieldConfig
                    {
                        Name = "",
                        Description = "",
                        IsIntrospection = true,
                        Resolver = () => (ctx, ct) => null
                    }),
                    new Field(new FieldConfig
                    {
                        Name = "",
                        Description = "",
                        IsIntrospection = true,
                        Resolver = () => (ctx, ct) => null
                    })
                }
            });

        }

        private static ObjectTypeConfig Create(SchemaContext schemaContext)
        {
            return new ObjectTypeConfig
            {
                Name = "__Schema",
                Description =
                    "A GraphQL Schema defines the capabilities of a GraphQL server. It " +
                    "exposes all available types and directives on the server, as well as " +
                    "the entry points for query, mutation, and subscription operations.",
                IsIntrospection = true,
                Fields = new[]
                {
                    new Field(new FieldConfig
                    {
                        Name = "types",
                        Description = "A list of all types supported by this server.",
                        IsIntrospection = true,
                        Type = () => new NonNullType(new ListType(new NonNullType(schemaContext.GetOutputType("__Type")))),
                        Resolver = () => (ctx, ct) => ctx.Schema.GetAllTypes()
                    }),
                    new Field(new FieldConfig
                    {
                        Name = "",
                        Description = "",
                        IsIntrospection = true,
                        Resolver = () => (ctx, ct) => null
                    }),
                    new Field(new FieldConfig
                    {
                        Name = "",
                        Description = "",
                        IsIntrospection = true,
                        Resolver = () => (ctx, ct) => null
                    }),
                    new Field(new FieldConfig
                    {
                        Name = "",
                        Description = "",
                        IsIntrospection = true,
                        Resolver = () => (ctx, ct) => null
                    })
                }
            }
        }
    }
}
