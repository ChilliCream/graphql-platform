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

        internal static IReadOnlyList<ITypeReference> CreateReferences(
            ITypeInspector typeInspector) =>
            new List<ITypeReference>
            {
                typeInspector.GetTypeRef(typeof(__Directive)),
                typeInspector.GetTypeRef(typeof(__DirectiveLocation)),
                typeInspector.GetTypeRef(typeof(__EnumValue)),
                typeInspector.GetTypeRef(typeof(__Field)),
                typeInspector.GetTypeRef(typeof(__InputValue)),
                typeInspector.GetTypeRef(typeof(__Schema)),
                typeInspector.GetTypeRef(typeof(__Type)),
                typeInspector.GetTypeRef(typeof(__TypeKind))
            };

        public static bool IsIntrospectionType(NameString typeName)
        {
            return typeName.HasValue
                && _typeNames.Contains(typeName.Value);
        }
    }
}
