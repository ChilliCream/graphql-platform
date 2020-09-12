using System;
using HotChocolate.Types;
using HotChocolate.Types.Relay.Descriptors;

namespace HotChocolate
{
    public static class NodeObjectTypeExtensions
    {
        public static INodeDescriptor AsNode(
            this IObjectTypeDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }
            return new NodeDescriptor(descriptor);
        }

        public static INodeDescriptor<T> AsNode<T>(
            this IObjectTypeDescriptor<T> descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }
            return new NodeDescriptor<T>(descriptor);
        }
    }
}
