namespace HotChocolate.Types.Relay.Descriptors
{
    public interface INodeResolverDescriptor<TNode, TId>
    {
        IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<TNode, TId> nodeResolver);
    }
}
