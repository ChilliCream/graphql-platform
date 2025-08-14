using System.CommandLine.Help;

namespace ChilliCream.Nitro.CLI.Results;

internal abstract class Result;

internal class ObjectResult(object value) : Result
{
    public object Value { get; } = value;
}

internal sealed class PaginatedListResult(object[] values, string cursor)
    : ObjectResult(new { Values = values, Cursor = cursor });
