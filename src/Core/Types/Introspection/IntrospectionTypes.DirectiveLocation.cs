using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Types.Introspection
{
    internal static partial class IntrospectionTypes
    {
        public static readonly Func<ISchemaContext, EnumTypeConfig<DirectiveLocation>> __DirectiveLocation = c => new EnumTypeConfig<DirectiveLocation>
        {
            Name = _directiveLocationName,
            Description =
                "A Directive can be adjacent to many parts of the GraphQL language, a " +
                "__DirectiveLocation describes one such possible adjacencies.",
            IsIntrospection = true,
            Values = new[]
            {
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Description = "Location adjacent to a query operation.",
                    Value = DirectiveLocation.Query
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Description = "Location adjacent to a mutation operation.",
                    Value = DirectiveLocation.Mutation
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Description = "Location adjacent to a subscription operation.",
                    Value = DirectiveLocation.Subscription
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Description = "Location adjacent to a field.",
                    Value = DirectiveLocation.Field
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Name = "FRAGMENT_DEFINITION",
                    Description = "Location adjacent to a fragment definition.",
                    Value = DirectiveLocation.FragmentDefinition
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Name = "FRAGMENT_SPREAD",
                    Description = "Location adjacent to a fragment spread.",
                    Value = DirectiveLocation.FragmentSpread
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Name = "INLINE_FRAGMENT",
                    Description = "Location adjacent to an inline fragment.",
                    Value = DirectiveLocation.InlineFragment
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Description = "Location adjacent to a schema definition.",
                    Value = DirectiveLocation.Schema
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Description = "Location adjacent to a scalar definition.",
                    Value = DirectiveLocation.Scalar
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Description = "Location adjacent to an object type definition.",
                    Value = DirectiveLocation.Object
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Name = "FIELD_DEFINITION",
                    Description = "Location adjacent to a field definition.",
                    Value = DirectiveLocation.FieldDefinition
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Name = "ARGUMENT_DEFINITION",
                    Description = "Location adjacent to an argument definition.",
                    Value = DirectiveLocation.ArgumentDefinition
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Description = "Location adjacent to an interface definition.",
                    Value = DirectiveLocation.Interface
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Description = "Location adjacent to a union definition.",
                    Value = DirectiveLocation.Union
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Description = "Location adjacent to an enum definition.",
                    Value = DirectiveLocation.Enum
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Name = "ENUM_VALUE",
                    Description = "Location adjacent to an enum value definition.",
                    Value = DirectiveLocation.EnumValue
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Name = "INPUT_OBJECT",
                    Description = "Location adjacent to an input object type definition.",
                    Value = DirectiveLocation.InputObject
                }),
                new EnumValue<DirectiveLocation>(new EnumValueConfig<DirectiveLocation>
                {
                    Name = "INPUT_FIELD_DEFINITION",
                    Description = "Location adjacent to an input object field definition.",
                    Value = DirectiveLocation.InputFieldDefinition
                })
            }


        };
    }
}
