namespace HotChocolate.Types.Pagination
{
    public interface IConnectionType : IObjectType
    {
        IEdgeType EdgeType { get; }
    }
}
