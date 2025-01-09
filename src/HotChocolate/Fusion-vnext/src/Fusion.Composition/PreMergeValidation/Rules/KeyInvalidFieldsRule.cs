using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

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
internal sealed class KeyInvalidFieldsRule : IEventHandler<KeyFieldsInvalidReferenceEvent>
{
    public void Handle(KeyFieldsInvalidReferenceEvent @event, CompositionContext context)
    {
        var (fieldNode, type, keyDirective, entityType, schema) = @event;

        context.Log.Write(
            KeyInvalidFields(
                entityType.Name,
                keyDirective,
                fieldNode.Name.Value,
                type.Name,
                schema));
    }
}
