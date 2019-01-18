namespace HotChocolate.Types.Relay
{
    public interface IConnectionType
        : IComplexOutputType
    {
        IEdgeType EdgeType { get; }
    }
}
