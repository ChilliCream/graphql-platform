namespace ChilliCream.Nitro.CommandLine.Helpers;

internal interface IPaginationPageInfo
{
    bool HasNextPage { get; }

    string? EndCursor { get; }
}
