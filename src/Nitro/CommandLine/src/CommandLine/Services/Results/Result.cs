namespace ChilliCream.Nitro.CommandLine.Results;

internal abstract class Result;

internal class ObjectResult(object value) : Result
{
    public object Value { get; } = value;
}

internal sealed class PaginatedListResult<TItem>(TItem[] values, string? cursor) : Result
{
    public TItem[] Values { get; } = values;

    public string? Cursor { get; } = cursor;
}
