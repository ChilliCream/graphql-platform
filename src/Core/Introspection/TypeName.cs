
using System;
using HotChocolate.Abstractions;

namespace HotChocolate.Introspection
{
    internal static class TypeName
    {
        public const string FieldName = "__typename";
        public static readonly FieldDefinition FieldDefinition =
            new FieldDefinition(FieldName, NamedType.NonNullString, true);

        public static bool IsTypeName(string fieldName)
        {
            return FieldName.Equals(fieldName, StringComparison.Ordinal);
        }
    }
}