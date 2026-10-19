namespace HotChocolate.Types.Pagination;

internal interface IStreamPageConnection
{
    ValueTask DisposeAsync();
}
