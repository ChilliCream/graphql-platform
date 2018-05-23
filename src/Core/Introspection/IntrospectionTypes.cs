using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Introspection
{
    internal static partial class IntrospectionTypes
    {
        private const string _schemaName = "__Schema";
        private const string _typeName = "__Type";
        private const string _typeKindName = "__TypeKind";
        private const string _fieldName = "__Field";
        private const string _enumValueName = "__EnumValue";
        private const string _directiveName = "__Directive";
        private const string _directiveLocationName = "__DirectiveLocation";
        private const string _inputValueName = "__InputValue";

        private static bool Contains<T>(IReadOnlyCollection<T> collection, T item)
        {
            foreach (T element in collection)
            {
                if (element.Equals(item))
                {
                    return true;
                }
            }
            return false;
        }

        public static readonly Func<ISchemaContext, Field> CreateSchemaField = c => new Field(new FieldConfig
        {
            Name = "__schema",
            Description = "Access the current type schema of this server.",
            Type = () => new NonNullType(c.GetOutputType(_schemaName)),
            Resolver = () => (ctx, ct) => ctx.Schema
        });

        public static readonly Func<ISchemaContext, Field> CreateTypeField = c => new Field(new FieldConfig
        {
            Name = "__type",
            Description = "Request the type information of a single type.",
            Type = () => c.GetOutputType(_typeName),
            Arguments = new[]
                {
                    new InputField(new InputFieldConfig
                    {
                        Name ="type",
                        Type = () => c.NonNullStringType()
                    })
                },
            Resolver = () => (ctx, ct) => ctx.Schema.GetType(ctx.Argument<string>("type"))
        });

        public static readonly Func<ISchemaContext, Field> CreateTypeNameField = c => new Field(new FieldConfig
        {
            Name = "__typename",
            Description = "The name of the current Object type at runtime.",
            Type = () => c.NonNullStringType(),
            Resolver = () => (ctx, ct) => ctx.ObjectType.Name
        });
    }
}
