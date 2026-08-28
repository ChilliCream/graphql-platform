namespace ChilliCream.Nitro.CommandLine.Results;

internal sealed record ListResult<TItem>(IReadOnlyList<TItem> Items) : Result;
