#nullable enable

namespace HotChocolate.Types
{
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

        public bool If { get; }

        public string? Label { get; }

        public int InitialCount { get; }
    }
}
