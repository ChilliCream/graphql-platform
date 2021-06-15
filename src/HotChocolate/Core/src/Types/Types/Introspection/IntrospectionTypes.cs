using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Introspection
{
    /// <summary>
    /// Helper to identify introspection types.
    /// </summary>
    public static class IntrospectionTypes
    {
        private static readonly HashSet<string> _typeNames =
            new()
            {
                __Directive.Names.__Directive,
                __DirectiveLocation.Names.__DirectiveLocation,
                __EnumValue.Names.__EnumValue,
                __Field.Names.__Field,
                __InputValue.Names.__InputValue,
                __Schema.Names.__Schema,
                __Type.Names.__Type,
                __TypeKind.Names.__TypeKind,
                __AppliedDirective.Names.__AppliedDirective,
                __DirectiveArgument.Names.__DirectiveArgument
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

        /// <summary>
        /// Defines if the type name represents an introspection type.
        /// </summary>
        public static bool IsIntrospectionType(NameString typeName) =>
            typeName.HasValue && _typeNames.Contains(typeName.Value);

        /// <summary>
        /// Defines if the type represents an introspection type.
        /// </summary>
        public static bool IsIntrospectionType(INamedType type) =>
            IsIntrospectionType(type.Name);
    }
}
