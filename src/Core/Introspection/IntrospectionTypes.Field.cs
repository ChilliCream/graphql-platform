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
        public static readonly Func<ISchemaContext, ObjectTypeConfig> __Field = c => new ObjectTypeConfig
        {
            Name = _fieldName,
            Description =
                "Object and Interface types are described by a list of Fields, each of " +
                "which has a name, potentially a list of arguments, and a return type.",
            IsIntrospection = true,
            Fields = new[]
            {
                new Field(new FieldConfig
                {
                    Name = "name",
                    Type = () => c.NonNullStringType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<Field>().Name
                }),
                new Field(new FieldConfig
                {
                    Name = "description",
                    Type = () => c.StringType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<Field>().Description
                }),
                new Field(new FieldConfig
                {
                    Name = "args",
                    Type = () => new NonNullType(new ListType(new NonNullType(c.GetOutputType(_inputValueName)))),
                    Resolver = () => (ctx, ct) => ctx.Parent<Field>().Arguments.Values
                }),
                new Field(new FieldConfig
                {
                    Name = "type",
                    Type = () => new NonNullType(c.GetOutputType(_typeName)),
                    Resolver = () => (ctx, ct) => ctx.Parent<Field>().Type
                }),
                new Field(new FieldConfig
                {
                    Name = "isDeprecated",
                    Type = () => c.NonNullBooleanType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<Field>().IsDeprecated
                }),
                new Field(new FieldConfig
                {
                    Name = "deprecationReason",
                    Type = () =>  c.StringType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<Field>().DeprecationReason
                })
            }
        };
    }
}
