namespace ChilliCream.Nitro.CommandLine.Results;

internal abstract record Result;

internal sealed record ObjectResult(object Value) : Result;

internal sealed record PaginatedListResult<TItem>(TItem[] Values, string? Cursor) : Result;
