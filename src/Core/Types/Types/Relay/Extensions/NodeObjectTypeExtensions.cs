using System;
using HotChocolate.Types.Relay.Descriptors;

namespace HotChocolate.Types.Relay
{
    public static class NodeObjectTypeExtensions
    {
        public static INodeDescriptor AsNode(
            this IObjectTypeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }
            return new NodeDescriptor(descriptor);
        }

        public static INodeDescriptor<T> AsNode<T>(
            this IObjectTypeDescriptor<T> descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }
            return new NodeDescriptor<T>(descriptor);
        }
    }
}
