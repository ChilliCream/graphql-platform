namespace HotChocolate.Types.Pagination;

internal sealed class PagingProviderEntry
{
    public PagingProviderEntry(string? name, CursorPagingProvider provider)
    {
        Name = name;
        Provider = provider;
    }

    public string? Name { get; }

    public CursorPagingProvider Provider { get; }
}
