using System;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using HotChocolate.Types.Relay.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate
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
