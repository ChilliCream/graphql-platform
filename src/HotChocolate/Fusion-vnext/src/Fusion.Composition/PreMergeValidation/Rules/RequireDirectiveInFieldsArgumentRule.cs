using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// The <c>@require</c> directive is used to specify fields on the same type that an argument
/// depends on in order to resolve the annotated field. When using <c>@require(fields: "…")</c>, the
/// <c>fields</c> argument must be a valid selection set string <b>without</b> any additional
/// directive applications. Applying a directive (e.g., <c>@lowercase</c>) inside this selection set
/// is not supported and triggers the <c>REQUIRE_DIRECTIVE_IN_FIELDS_ARG</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Require-Directive-in-Fields-Argument">
/// Specification
/// </seealso>
internal sealed class RequireDirectiveInFieldsArgumentRule : IEventHandler<RequireFieldNodeEvent>
{
    public void Handle(RequireFieldNodeEvent @event, CompositionContext context)
    {
        var (fieldNode, fieldNamePath, requireDirective, argument, field, type, schema) = @event;

        if (fieldNode.Directives.Count != 0)
        {
            context.Log.Write(
                RequireDirectiveInFieldsArgument(
                    fieldNamePath,
                    requireDirective,
                    argument.Name,
                    field.Name,
                    type.Name,
                    schema));
        }
    }
}
