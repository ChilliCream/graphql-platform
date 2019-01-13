namespace HotChocolate.Types.Paging
{
    public interface IEdgeType
        : IComplexOutputType
    {
        INamedOutputType EntityType { get; }
    }
}
