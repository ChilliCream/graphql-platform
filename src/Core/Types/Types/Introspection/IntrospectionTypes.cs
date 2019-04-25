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
                new ClrTypeReference(
                    typeof(__Directive),
                    TypeContext.Output),
                new ClrTypeReference(
                    typeof(__DirectiveLocation),
                    TypeContext.None),
                new ClrTypeReference(
                    typeof(__EnumValue),
                    TypeContext.Output),
                new ClrTypeReference(
                    typeof(__Field),
                    TypeContext.Output),
                new ClrTypeReference(
                    typeof(__InputValue),
                    TypeContext.Output),
                new ClrTypeReference(
                    typeof(__Schema),
                    TypeContext.Output),
                new ClrTypeReference(
                    typeof(__Type),
                    TypeContext.Output),
                new ClrTypeReference(
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
