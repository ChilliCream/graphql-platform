namespace HotChocolate.Types.Relay
{
    public interface IEdgeType
        : IComplexOutputType
    {
        INamedOutputType EntityType { get; }
    }
}
