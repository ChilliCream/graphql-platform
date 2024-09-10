namespace HotChocolate.Types.Pagination;

internal sealed class PagingProviderEntry(string? name, CursorPagingProvider provider)
{
    public string? Name { get; } = name;

    public CursorPagingProvider Provider { get; } = provider;
}
