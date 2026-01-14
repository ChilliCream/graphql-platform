using HotChocolate.Types.Relay.Descriptors;

namespace HotChocolate.Types;

public static class NodeObjectTypeExtensions
{
    public static INodeDescriptor ImplementsNode(
        this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return new NodeDescriptor(descriptor);
    }

    public static INodeDescriptor<T> ImplementsNode<T>(
        this IObjectTypeDescriptor<T> descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return new NodeDescriptor<T>(descriptor);
    }
}
