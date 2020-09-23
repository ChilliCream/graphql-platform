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
    public class DeferDirectiveType
        : DirectiveType<DeferDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<DeferDirective> descriptor)
        {
            descriptor
                .Name("defer")
                .Description(
                    "The `@defer` directive may be provided for fragment spreads and " +
                    "inline fragments to inform the executor to delay the execution of " +
                    "the current fragment to indicate deprioritization of the current fragment." +
                    "A query with `@defer` directive will cause the request to potentially " +
                    "return multiple responses, where non-deferred data is delivered in " +
                    "the initial response and data deferred is delivered in a subsequent " +
                    "response. `@include` and `@skip` take precedence over `@defer`.")
                .Location(
                    DirectiveLocation.FragmentSpread |
                    DirectiveLocation.InlineFragment);

            descriptor
                .Argument(t => t.Label)
                .Name(WellKnownDirectives.LabelArgument)
                .Description(
                    "If this argument label has a value other than null, it will be passed " +
                    "on to the result of this defer directive. This label is intended to " +
                    "give client applications a way to identify to which fragment a deferred " +
                    "result belongs to.")
                .Type<StringType>();

            descriptor
                .Argument(t => t.If)
                .Name(WellKnownDirectives.IfArgument)
                .Description("Deferred when true.")
                .Type<BooleanType>();
        }
    }
}
