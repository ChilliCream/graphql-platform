using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// <para>
/// The <c>@key</c> directive specifies the set of fields used to uniquely identify an entity. The
/// <c>fields</c> argument must be valid and meet the following conditions:
/// </para>
/// <para>
/// 1. It must have valid GraphQL syntax.<br/>
/// 2. It must reference fields that are defined on the annotated type.
/// </para>
/// <para>
/// Violations of these conditions result in an invalid schema composition, as the entity key cannot
/// be properly resolved.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Key-Invalid-Fields">
/// Specification
/// </seealso>
internal sealed class KeyInvalidFieldsRule
    : IEventHandler<KeyFieldsInvalidReferenceEvent>
    , IEventHandler<KeyFieldsInvalidSyntaxEvent>
{
    public void Handle(KeyFieldsInvalidReferenceEvent @event, CompositionContext context)
    {
        var (entityType, keyDirective, fieldNode, type, schema) = @event;

        context.Log.Write(
            KeyInvalidFieldsReference(
                entityType.Name,
                keyDirective,
                fieldNode.Name.Value,
                type.Name,
                schema));
    }

    public void Handle(KeyFieldsInvalidSyntaxEvent @event, CompositionContext context)
    {
        var (entityType, keyDirective, schema) = @event;

        context.Log.Write(KeyInvalidFieldsSyntax(entityType.Name, keyDirective, schema));
    }
}
