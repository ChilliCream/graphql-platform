namespace ChilliCream.Nitro.CommandLine.Helpers;

internal sealed class SimplePageInfo(string? endCursor, bool hasNextPage) : IPaginationPageInfo
{
    public bool HasNextPage { get; } = hasNextPage;

    public string? EndCursor { get; } = endCursor;
}
