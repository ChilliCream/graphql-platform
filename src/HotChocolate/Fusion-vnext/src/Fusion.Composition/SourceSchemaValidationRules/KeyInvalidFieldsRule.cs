using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Validators;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Even if the selection set for <c>@key(fields: "…")</c> is syntactically valid, field references
/// within that selection set must also refer to <b>actual</b> fields on the annotated type. This
/// includes nested selections, which must appear on the corresponding return type. If any
/// referenced field is missing or incorrectly named, composition fails with a
/// <c>KEY_INVALID_FIELDS</c> error because the entity key cannot be resolved correctly.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Key-Invalid-Fields">
/// Specification
/// </seealso>
internal sealed class KeyInvalidFieldsRule : IEventHandler<KeyFieldsEvent>
{
    public void Handle(KeyFieldsEvent @event, CompositionContext context)
    {
        var (selectionSet, keyDirective, entityType, schema) = @event;

        var validator = new SelectionSetValidator(schema);
        var errors = validator.Validate(selectionSet, entityType);

        if (errors.Any())
        {
            context.Log.Write(
                KeyInvalidFields(
                    keyDirective,
                    entityType.Name,
                    schema,
                    errors));
        }
    }
}
