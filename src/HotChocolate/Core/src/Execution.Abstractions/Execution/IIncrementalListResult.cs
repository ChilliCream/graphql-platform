namespace HotChocolate.Execution;

/// <summary>
/// Represents an incremental result that delivers additional list items for a @stream directive.
/// </summary>
public interface IIncrementalListResult : IIncrementalResult
{
    /// <summary>
    /// Gets the additional list items to append to the streamed list field.
    /// </summary>
    IReadOnlyList<object?> Items { get; }
}
