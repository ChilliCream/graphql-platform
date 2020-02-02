namespace HotChocolate.Types.Relay
{
    public interface IPageInfo
    {
        bool HasNextPage { get; }

        bool HasPreviousPage { get; }

        string StartCursor { get; }

        string EndCursor { get; }

        long? TotalCount { get; }
    }
}
