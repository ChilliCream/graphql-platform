using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting
{
    internal sealed class TypeReferenceComparer
        : IEqualityComparer<ITypeReference>
    {
        public bool Equals(ITypeReference? x, ITypeReference? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
            {
                return false;
            }

            if (x.Context != y.Context &&
                x.Context != TypeContext.None &&
                y.Context != TypeContext.None)
            {
                return false;
            }

            if (!string.Equals(x.Scope, y.Scope, StringComparison.Ordinal))
            {
                return false;
            }

            if (x is SchemaTypeReference xSchemaType &&
                y is ExtendedTypeReference yExtendedTypeReference)
            {
                return CompareSchemaAndExtendedTypeRef(xSchemaType, yExtendedTypeReference);
            }

            if (y is SchemaTypeReference ySchemaType &&
                x is ExtendedTypeReference xExtendedTypeReference)
            {
                return CompareSchemaAndExtendedTypeRef(ySchemaType, xExtendedTypeReference);
            }

            return x.Equals(y);
        }

        public int GetHashCode(ITypeReference obj)
        {
            unchecked
            {
                var hashCode = obj.Context.GetHashCode();

                if (obj.Scope is not null)
                {
                    hashCode ^= obj.GetHashCode() * 397;
                }

                if (obj is SchemaTypeReference schemaTypeReference)
                {
                    hashCode ^= schemaTypeReference.Type.GetType().GetHashCode() * 397;
                }

                if (obj is ExtendedTypeReference extendedTypeReference)
                {
                    hashCode ^= extendedTypeReference.Type.Source.GetHashCode() * 397;
                }

                return hashCode;
            }
        }

        private static bool CompareSchemaAndExtendedTypeRef(
            SchemaTypeReference schemaTypeReference,
            ExtendedTypeReference extendedTypeReference) =>
            schemaTypeReference.Type.GetType() == extendedTypeReference.Type.Source;

        public static readonly IEqualityComparer<ITypeReference> Default =
            new TypeReferenceComparer();
    }
}
