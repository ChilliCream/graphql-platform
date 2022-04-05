namespace HotChocolate.Types.Pagination;

internal sealed class PagingProviderEntry
{
    public PagingProviderEntry(string? name, OffsetPagingProvider provider)
    {
        Name = name;
        Provider = provider;
    }

    public string? Name { get; }

    public OffsetPagingProvider Provider { get; }
}
