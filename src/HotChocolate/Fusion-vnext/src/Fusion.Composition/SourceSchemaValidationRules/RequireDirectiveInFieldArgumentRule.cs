using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@require</c> directive is used to specify fields on the same type that an argument
/// depends on in order to resolve the annotated field. When using <c>@require(field: "…")</c>, the
/// <c>field</c> argument must be a valid selection set string <b>without</b> any additional
/// directive applications. Applying a directive (e.g., <c>@lowercase</c>) inside this selection set
/// is not supported and triggers the <c>REQUIRE_DIRECTIVE_IN_FIELD_ARG</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Require-Directive-in-Field-Argument">
/// Specification
/// </seealso>
internal sealed class RequireDirectiveInFieldArgumentRule : IEventHandler<RequireFieldNodeEvent>
{
    public void Handle(RequireFieldNodeEvent @event, CompositionContext context)
    {
        var (fieldNode, fieldNamePath, requireDirective, argument, field, type, schema) = @event;

        if (fieldNode.Directives.Count != 0)
        {
            context.Log.Write(
                RequireDirectiveInFieldArgument(
                    fieldNamePath,
                    requireDirective,
                    argument.Name,
                    field.Name,
                    type.Name,
                    schema));
        }
    }
}
