using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Helpers;

public static class TypeReferenceHelper
{
    /// <summary>
    /// Returns the same type, but its nullable counterpart,
    /// effectively removing NonNull wrapping.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// The system did not know how to change the nullability of the provided instance
    /// </exception>
    public static TypeReference GetNullableAnalogue(
        this TypeReference reference,
        ITypeInspector typeInspector)
    {
        if (reference is ExtendedTypeReference extendedTypeRef)
        {
            return extendedTypeRef.Type.IsNullable
                ? extendedTypeRef
                : extendedTypeRef.WithType(
                    typeInspector.ChangeNullability(extendedTypeRef.Type, true));
        }

        if (reference is SchemaTypeReference schemaRef)
        {
            return schemaRef.Type is NonNullType nnt
                ? schemaRef.WithType(nnt.Type)
                : schemaRef;
        }

        if (reference is SyntaxTypeReference syntaxRef)
        {
            return syntaxRef.Type is NonNullTypeNode nnt
                ? syntaxRef.WithType(nnt.Type)
                : syntaxRef;
        }

        throw new NotSupportedException();
    }
}
