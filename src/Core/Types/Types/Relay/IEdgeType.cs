namespace HotChocolate.Types.Relay
{
    public interface IEdgeType
        : IComplexOutputType
    {
        IOutputType EntityType { get; }
    }
}
