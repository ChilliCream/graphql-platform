#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// The `@defer` directive may be provided for fragment spreads and inline fragments to
    /// inform the executor to delay the execution of the current fragment to indicate
    /// deprioritization of the current fragment. A query with `@defer` directive will cause
    /// the request to potentially return multiple responses, where non-deferred data is
    /// delivered in the initial response and data deferred is delivered in a subsequent
    /// response. `@include` and `@skip` take precedence over `@defer`.
    ///
    /// directive @defer(label: String, if: Boolean) on FRAGMENT_SPREAD | INLINE_FRAGMENT
    /// </summary>
    public class DeferDirective
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DeferDirective"/>
        /// </summary>
        public DeferDirective(bool @if, string? label)
        {
            If = @if;
            Label = label;
        }

        /// <summary>
        /// If this argument label has a value other than null, it will be passed
        /// on to the result of this defer directive. This label is intended to
        /// give client applications a way to identify to which fragment a deferred
        /// result belongs to.
        /// </summary>
        public bool If { get; }

        /// <summary>
        /// Deferred when true.
        /// </summary>
        public string? Label { get; }
    }
}
