namespace ChilliCream.Nitro.CommandLine.Results;

internal sealed record PaginatedListResult<TItem>(TItem[] Values, string? Cursor) : Result;
