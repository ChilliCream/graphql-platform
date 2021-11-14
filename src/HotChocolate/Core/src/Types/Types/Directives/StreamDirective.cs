#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The `@stream` directive may be provided for a field of `List` type so that the
/// backend can leverage technology such as asynchronous iterators to provide a partial
/// list in the initial response, and additional list items in subsequent responses.
/// `@include` and `@skip` take precedence over `@stream`.
///
/// directive @stream(label: String, initialCount: Int!, if: Boolean) on FIELD
/// </summary>
public sealed class StreamDirective
{
    /// <summary>
    /// Initializes a new instance of <see cref="StreamDirective"/>
    /// </summary>
    public StreamDirective(bool @if, int initialCount, string? label = null)
    {
        If = @if;
        InitialCount = initialCount;
        Label = label;
    }

    /// <summary>
    /// Streamed when true.
    /// </summary>
    public bool If { get; }

    /// <summary>
    /// If this argument label has a value other than null,
    /// it will be passed on to the result of this stream directive.
    /// This label is intended to give client applications a way to identify to
    /// which fragment a streamed result belongs to.
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// The initial elements that shall be send down to the consumer.
    /// </summary>
    public int InitialCount { get; }
}
