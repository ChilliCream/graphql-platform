namespace HotChocolate.Types.Relay.Descriptors
{
    public interface INodeDescriptor
    {
        IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<object, object> nodeResolver);

        IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<object, TId> nodeResolver);
    }
}
