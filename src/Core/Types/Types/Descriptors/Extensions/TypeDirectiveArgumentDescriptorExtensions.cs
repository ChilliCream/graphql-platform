using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public static class TypeDirectiveArgumentDescriptorExtensions
    {
        public static IDirectiveArgumentDescriptor Type(
            this IDirectiveArgumentDescriptor descriptor,
            NameString typeName,
            Func<NamedTypeNode, ITypeNode> typeConfig = null)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (typeConfig is null)
            {
                return descriptor.Type(new NamedTypeNode(typeName));
            }

            return descriptor.Type(typeConfig(new NamedTypeNode(typeName)));
        }

        public static IDirectiveArgumentDescriptor NonNullType(
            this IDirectiveArgumentDescriptor descriptor,
            NameString typeName,
            Func<NonNullTypeNode, ITypeNode> typeConfig = null)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var nonNullType = new NonNullTypeNode(new NamedTypeNode(typeName));

            if (typeConfig is null)
            {
                return descriptor.Type(nonNullType);
            }

            return descriptor.Type(typeConfig(nonNullType));
        }
    }
}
