namespace HotChocolate.Types.Paging
{
    public interface IEdge
    {
        string Cursor { get; }
        object Node { get; }
    }
}
