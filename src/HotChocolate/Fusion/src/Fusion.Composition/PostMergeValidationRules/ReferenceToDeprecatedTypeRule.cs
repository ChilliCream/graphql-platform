using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// In a composed schema, a field that is not deprecated must not reference a deprecated object
/// type. Merging propagates the deprecation of an object type from any source schema that declares
/// it, so the composed schema can pair a deprecated type with a field that no source schema
/// deprecated.
/// </summary>
internal sealed class ReferenceToDeprecatedTypeRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (field.IsDeprecated)
        {
            return;
        }

        if (field.Type.NamedType() is IObjectTypeDefinition { IsDeprecated: true } objectType)
        {
            context.Log.Write(
                ReferenceToDeprecatedType(
                    field,
                    type.Name,
                    objectType.Name,
                    schema));
        }
    }
}
