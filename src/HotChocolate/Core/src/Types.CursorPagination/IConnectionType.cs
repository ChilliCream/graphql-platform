namespace HotChocolate.Types.Relay
{
    public interface IConnectionType : IObjectType
    {
        IEdgeType EdgeType { get; }
    }
}
