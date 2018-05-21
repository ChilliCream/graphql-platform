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
        public static readonly Func<ISchemaContext, ObjectTypeConfig> __InputValue = c => new ObjectTypeConfig
        {
            Name = _inputValueName,
            Description =
                "Arguments provided to Fields or Directives and the input fields of an " +
                "InputObject are represented as Input Values which describe their type " +
                "and optionally a default value.",
            IsIntrospection = true,
            Fields = new[]
            {
                new Field(new FieldConfig
                {
                    Name = "name",
                    Type = () => c.NonNullStringType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<InputField>().Name
                }),
                new Field(new FieldConfig
                {
                    Name = "description",
                    Type = () => c.StringType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<InputField>().Description
                }),
                new Field(new FieldConfig
                {
                    Name = "type",
                    Type = () => new NonNullType(c.GetOutputType(_typeName)),
                    Resolver = () => (ctx, ct) => ctx.Parent<InputField>().Type
                }),
                new Field(new FieldConfig
                {
                    Name = "defaultValue",
                    Description =
                        "A GraphQL-formatted string representing the default value for this " +
                        "input value.",
                    Type = () => c.StringType(),
                    Resolver = () => (ctx, ct) => ctx.Parent<InputField>().IsDeprecated
                })
            }
        };
    }
}
