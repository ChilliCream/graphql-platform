using System;
using HotChocolate.Types.Relay.Descriptors;

namespace HotChocolate.Types
{
    public static class NodeObjectTypeExtensions
    {
        public static INodeDescriptor ImplementsNode(
            this IObjectTypeDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new NodeDescriptor(descriptor);
        }

        public static INodeDescriptor<T> ImplementsNode<T>(
            this IObjectTypeDescriptor<T> descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new NodeDescriptor<T>(descriptor);
        }

        [Obsolete("Use ImplementsNode.")]
        public static INodeDescriptor AsNode(
            this IObjectTypeDescriptor descriptor) =>
            ImplementsNode(descriptor);

        [Obsolete("Use ImplementsNode.")]
        public static INodeDescriptor<T> AsNode<T>(
            this IObjectTypeDescriptor<T> descriptor) =>
            ImplementsNode<T>(descriptor);
    }
}
