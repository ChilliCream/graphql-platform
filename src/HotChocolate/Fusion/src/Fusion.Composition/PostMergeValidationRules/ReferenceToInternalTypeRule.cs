using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// In a composed schema, fields must not reference internal types. This requirement guarantees that
/// public types do not reference internal structures which are intended for internal use.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Reference-To-Internal-Type">
/// Specification
/// </seealso>
internal sealed class ReferenceToInternalTypeRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        var fieldType = field.Type.AsTypeDefinition();

        if (schema.Types.TryGetType(fieldType.Name, out var typeDef) && typeDef is InternalMutableObjectTypeDefinition)
        {
            context.Log.Write(
                ReferenceToInternalType(
                    field,
                    type.Name,
                    fieldType.Name,
                    schema));
        }
    }
}
