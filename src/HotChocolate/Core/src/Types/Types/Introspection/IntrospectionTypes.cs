using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Introspection
{
    public static class IntrospectionTypes
    {
        private static readonly HashSet<string> _typeNames =
            new HashSet<string>
            {
                "__Directive",
                "__DirectiveLocation",
                "__EnumValue",
                "__Field",
                "__InputValue",
                "__Schema",
                "__Type",
                "__TypeKind"
            };

        internal static IReadOnlyList<ITypeReference> All { get; } =
            new List<ITypeReference>
            {
                TypeReference.Create(
                    typeof(__Directive),
                    TypeContext.Output),
                TypeReference.Create(
                    typeof(__DirectiveLocation),
                    TypeContext.None),
                TypeReference.Create(
                    typeof(__EnumValue),
                    TypeContext.Output),
                TypeReference.Create(
                    typeof(__Field),
                    TypeContext.Output),
                TypeReference.Create(
                    typeof(__InputValue),
                    TypeContext.Output),
                TypeReference.Create(
                    typeof(__Schema),
                    TypeContext.Output),
                TypeReference.Create(
                    typeof(__Type),
                    TypeContext.Output),
                TypeReference.Create(
                    typeof(__TypeKind),
                    TypeContext.None),
            };

        public static bool IsIntrospectionType(NameString typeName)
        {
            return typeName.HasValue
                && _typeNames.Contains(typeName.Value);
        }
    }
}
