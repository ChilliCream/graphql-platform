using System;
using HotChocolate.Types;

namespace HotChocolate.Introspection
{
    internal static partial class IntrospectionTypes
    {
        public static readonly Func<ISchemaContext, ObjectTypeConfig> __Schema = c => new ObjectTypeConfig
        {
            Name = _schemaName,
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
                    Type = () => new NonNullType(new ListType(new NonNullType(c.GetOutputType(_typeName)))),
                    Resolver = () => (ctx, ct) => ctx.Schema.GetAllTypes()
                }),
                new Field(new FieldConfig
                {
                    Name = "queryType",
                    Description = "The type that query operations will be rooted at.",
                    Type = () => new NonNullType(c.GetOutputType(_typeName)),
                    Resolver = () => (ctx, ct) => ctx.Schema.QueryType
                }),
                new Field(new FieldConfig
                {
                    Name = "mutationType",
                    Description =
                        "If this server supports mutation, the type that " +
                        "mutation operations will be rooted at.",
                    Type = () => new NonNullType(c.GetOutputType(_typeName)),
                    Resolver = () => (ctx, ct) => ctx.Schema.MutationType
                }),
                new Field(new FieldConfig
                {
                    Name = "description",
                    Description =
                        "If this server support subscription, the type that " +
                        "subscription operations will be rooted at.",
                    Type = () => new NonNullType(c.GetOutputType(_typeName)),
                    Resolver = () => (ctx, ct) => ctx.Schema.SubscriptionType
                }),
                new Field(new FieldConfig
                {
                    Name = "directives",
                    Description = "A list of all directives supported by this server.",
                    Type = () => new NonNullType(new ListType(new NonNullType(c.GetOutputType(_directiveName)))),
                    Resolver = () => (ctx, ct) => ctx.Schema.GetDirectives()
                })
            }
        };
    }
}
