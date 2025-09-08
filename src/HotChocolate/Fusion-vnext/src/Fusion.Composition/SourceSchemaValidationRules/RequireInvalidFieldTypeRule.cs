using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// When using the <c>@require</c> directive, the <c>field</c> argument must always be a string that
/// defines a (potentially nested) selection set of fields from the same type. If the <c>field</c>
/// argument is provided as a type other than a string (such as an integer, boolean, or enum), the
/// directive usage is invalid and will cause schema composition to fail.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Require-Invalid-Field-Type">
/// Specification
/// </seealso>
internal sealed class RequireInvalidFieldTypeRule : IEventHandler<RequireFieldInvalidTypeEvent>
{
    public void Handle(RequireFieldInvalidTypeEvent @event, CompositionContext context)
    {
        var (requireDirective, argument, field, type, schema) = @event;

        context.Log.Write(
            RequireInvalidFieldType(
                requireDirective,
                argument.Name,
                field.Name,
                type.Name,
                schema));
    }
}
