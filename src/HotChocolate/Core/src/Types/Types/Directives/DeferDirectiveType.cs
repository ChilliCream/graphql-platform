using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

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
public sealed class DeferDirectiveType : DirectiveType<DeferDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<DeferDirective> descriptor)
    {
        descriptor
            .Name(DirectiveNames.Defer.Name)
            .Description(TypeResources.DeferDirectiveType_Description)
            .Location(DirectiveLocation.FragmentSpread)
            .Location(DirectiveLocation.InlineFragment);

        descriptor
            .Argument(t => t.Label)
            .Name(DirectiveNames.Defer.Arguments.Label)
            .Description(TypeResources.DeferDirectiveType_Label_Description)
            .Type<StringType>();

        descriptor
            .Argument(t => t.If)
            .Name(DirectiveNames.Defer.Arguments.If)
            .Description(TypeResources.DeferDirectiveType_If_Description)
            .Type<BooleanType>();
    }
}
