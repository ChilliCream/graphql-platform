namespace HotChocolate.Types.Paging
{
    public interface IConnectionType
        : IComplexOutputType
    {
        IEdgeType EdgeType { get; }
    }
}
