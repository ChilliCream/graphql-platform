namespace HotChocolate.Types.Relay
{
    public interface IEdge
    {
        string Cursor { get; }
        object Node { get; }
    }
}
