namespace ChilliCream.Nitro.CommandLine.Cloud.Results;

internal abstract class Result;

internal class ObjectResult(object value) : Result
{
    public object Value { get; } = value;
}

internal sealed record PaginatedListResult<TItem>(TItem[] Values, string? Cursor);
