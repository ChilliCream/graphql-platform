using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal sealed class TypeReference
    {
        public TypeReference(ITypeNode type)
        {
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
        }

        public TypeReference(Type nativeType)
        {
            NativeType = nativeType
                ?? throw new ArgumentNullException(nameof(nativeType));
        }

        public Type NativeType { get; }
        public ITypeNode Type { get; }
    }

    internal static class TypeReferenceExtensions
    {
        public static bool IsNativeTypeReference(
            this TypeReference typeReference)
        {
            return typeReference.NativeType != null;
        }
    }
}
