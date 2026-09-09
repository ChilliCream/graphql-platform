using System.Collections.Immutable;

namespace HotChocolate.Execution;

/// <summary>
/// Represents an incremental result that delivers one additional list item for a @stream directive.
/// </summary>
public sealed class IncrementalListResult : IIncrementalListResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="IncrementalListResult"/>.
    /// </summary>
    /// <param name="id">The unique identifier that correlates this result with its pending entry.</param>
    /// <param name="item">The formatted item to append to the streamed list field.</param>
    /// <param name="errors">The GraphQL errors that occurred while resolving the streamed item.</param>
    /// <param name="extensions">Additional information associated with this incremental result.</param>
    public IncrementalListResult(
        int id,
        OperationResultData item,
        ImmutableList<IError>? errors = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        Id = id;
        Items = [item];
        Errors = errors ?? [];
        Extensions = extensions;
    }

    /// <summary>
    /// Gets the unique identifier that correlates this incremental result with its corresponding pending entry.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the formatted item to append to the streamed list field.
    /// </summary>
    public IReadOnlyList<OperationResultData> Items { get; }

    /// <summary>
    /// Gets the GraphQL errors that occurred while resolving the streamed item.
    /// </summary>
    public ImmutableList<IError> Errors { get; }

    /// <summary>
    /// Gets additional information associated with this incremental result, or <c>null</c> when none is present.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; }
}
