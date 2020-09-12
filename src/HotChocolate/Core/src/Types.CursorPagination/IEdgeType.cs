namespace HotChocolate.Types.Relay
{
    public interface IEdgeType : IObjectType
    {
        IOutputType EntityType { get; }
    }
}
