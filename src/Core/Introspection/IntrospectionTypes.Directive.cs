using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Introspection
{
    internal static partial class IntrospectionTypes
    {
        public static readonly Func<ISchemaContext, ObjectTypeConfig> __Directive = c => new ObjectTypeConfig
        {
            Name = _directiveName,
            Description =
                "A Directive provides a way to describe alternate runtime execution and " +
                "type validation behavior in a GraphQL document." +
                "\n\nIn some cases, you need to provide options to alter GraphQL's " +
                "execution behavior in ways field arguments will not suffice, such as " +
                "conditionally including or skipping a field. Directives provide this by " +
                "describing additional information to the executor.",
            IsIntrospection = true,
            Fields = new[]
            {
                new Field(new FieldConfig
                {
                    Name = "name",
                    Type = c.NonNullStringType,
                    Resolver = () => (ctx, ct) => ctx.Parent<Directive>().Name
                }),
                new Field(new FieldConfig
                {
                    Name = "description",
                    Type = c.StringType,
                    Resolver = () => (ctx, ct) => ctx.Parent<Directive>().Description
                }),
                new Field(new FieldConfig
                {
                    Name = "locations",
                    Type = () => new NonNullType(new ListType(
                        new NonNullType(c.GetOutputType(_directiveLocationName)))),
                    Resolver = () => (ctx, ct) => ctx.Parent<Directive>().Locations
                }),
                new Field(new FieldConfig
                {
                    Name = "args",
                    Type = () => new NonNullType(new ListType(
                        new NonNullType(c.GetOutputType(_typeName)))),
                    Resolver = () => (ctx, ct) => ctx.Parent<Directive>().Arguments
                }),
                new Field(new FieldConfig
                {
                    Name = "onOperation",
                    DeprecationReason = "Use `locations`.",
                    Type = () => c.NonNullBooleanType(),
                    Resolver = () => (ctx, ct) =>
                    {
                        IReadOnlyCollection<DirectiveLocation> locations =
                            ctx.Parent<Directive>().Locations;
                        return Contains(locations, DirectiveLocation.Query)
                            || Contains(locations, DirectiveLocation.Mutation)
                            || Contains(locations, DirectiveLocation.Subscription);
                    }
                }),
                new Field(new FieldConfig
                {
                    Name = "onFragment",
                    DeprecationReason = "Use `locations`.",
                    Type = () => new NonNullType(c.GetOutputType(_directiveName)),
                    Resolver = () => (ctx, ct) =>
                    {
                        IReadOnlyCollection<DirectiveLocation> locations =
                            ctx.Parent<Directive>().Locations;
                        return Contains(locations, DirectiveLocation.InlineFragment)
                            || Contains(locations, DirectiveLocation.FragmentSpread)
                            || Contains(locations, DirectiveLocation.FragmentDefinition);
                    }
                }),
                new Field(new FieldConfig
                {
                    Name = "onField",
                    DeprecationReason = "Use `locations`.",
                    Type = () => new NonNullType(c.GetOutputType(_directiveName)),
                    Resolver = () => (ctx, ct) =>
                    {
                        IReadOnlyCollection<DirectiveLocation> locations =
                            ctx.Parent<Directive>().Locations;
                        return Contains(locations, DirectiveLocation.Field);
                    }
                })
            }
        };
    }
}
