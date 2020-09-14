namespace HotChocolate.Types.Pagination
{
    public interface IEdgeType : IObjectType
    {
        IOutputType EntityType { get; }
    }
}
