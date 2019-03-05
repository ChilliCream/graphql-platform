using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public static class TypeReferenceExtensions
    {
        public static ITypeReference GetMoreSpecific(
            this ITypeReference typeReference,
            Type type,
            TypeContext context)
        {
            throw new NotImplementedException();
        }

        public static ITypeReference GetMoreSpecific(
            this ITypeReference typeReference,
            ITypeNode typeNode)
        {
            throw new NotImplementedException();
        }
    }
}
