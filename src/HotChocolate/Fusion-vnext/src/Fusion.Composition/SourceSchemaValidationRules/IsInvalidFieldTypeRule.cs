using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// When using the <c>@is</c> directive, the <c>field</c> argument must always be a string that
/// describes how the arguments can be mapped from the entity type that the lookup field resolves.
/// If the <c>field</c> argument is provided as a type other than a string (such as an integer,
/// boolean, or enum), the directive usage is invalid and will cause schema composition to fail.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Is-Invalid-Field-Type">
/// Specification
/// </seealso>
internal sealed class IsInvalidFieldTypeRule : IEventHandler<IsFieldInvalidTypeEvent>
{
    public void Handle(IsFieldInvalidTypeEvent @event, CompositionContext context)
    {
        var (isDirective, argument, field, type, schema) = @event;

        context.Log.Write(
            IsInvalidFieldType(
                isDirective,
                argument.Name,
                field.Name,
                type.Name,
                schema));
    }
}
