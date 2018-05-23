using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Introspection
{
    internal static partial class IntrospectionTypes
    {
        public static readonly Func<ISchemaContext, ObjectTypeConfig> __EnumValue = c => new ObjectTypeConfig
        {
            Name = _enumValueName,
            Description =
                "One possible value for a given Enum. Enum values are unique values, not " +
                "a placeholder for a string or numeric value. However an Enum value is " +
                "returned in a JSON response as a string.",
            IsIntrospection = true,
            Fields = new[]
            {
                new Field(new FieldConfig
                {
                    Name = "name",
                    Type = () => c.NonNullStringType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<EnumValue>().Name
                }),
                new Field(new FieldConfig
                {
                    Name = "description",
                    Type = () => c.StringType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<EnumValue>().Description
                }),
                new Field(new FieldConfig
                {
                    Name = "isDeprecated",
                    Type = () => c.NonNullBooleanType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<EnumValue>().IsDeprecated
                }),
                new Field(new FieldConfig
                {
                    Name = "deprecationReason",
                    Type = () => c.StringType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<EnumValue>().DeprecationReason
                })
            }
        };
    }
}
