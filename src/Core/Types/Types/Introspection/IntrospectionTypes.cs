using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Introspection
{
    internal static class IntrospectionTypes
    {
        public static IReadOnlyList<ITypeReference> All { get; } =
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
    }
}
