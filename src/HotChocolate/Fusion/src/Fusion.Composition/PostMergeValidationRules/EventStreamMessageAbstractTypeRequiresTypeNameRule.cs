using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// Validates that a <c>@fusion__eventStream</c> message includes <c>__typename</c> at every abstract
/// type position (interface or union).
/// </summary>
/// <remarks>
/// <para>
/// A broker event payload has no subgraph round-trip, so the runtime resolves the concrete type of
/// an abstract position from the <c>__typename</c> carried by the message body. When it is omitted,
/// the concrete type cannot be resolved at runtime, so its presence is required at composition time.
/// </para>
/// <para>
/// The check runs on the merged schema so that merge diagnostics are reported first; an unmergeable
/// event-stream field never reaches this rule.
/// </para>
/// </remarks>
internal sealed class EventStreamMessageAbstractTypeRequiresTypeNameRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, mergedSchema) = @event;

        foreach (var schemaName in field.GetFusionEventStreamSchemaNames())
        {
            var message = field.GetFusionEventStreamMessage(schemaName);

            if (message is null)
            {
                continue;
            }

            if (MissingTypeName(message, field.Type.AsTypeDefinition(), mergedSchema))
            {
                var sourceSchema =
                    context.SchemaDefinitions.FirstOrDefault(s => s.Name == schemaName) ?? mergedSchema;

                context.Log.Write(EventStreamMessageAbstractTypeRequiresTypeName(field, sourceSchema));
            }
        }
    }

    private static bool MissingTypeName(
        SelectionSetNode selectionSet,
        ITypeDefinition type,
        ISchemaDefinition schema)
    {
        if (type.IsAbstractType() && !SelectsTypeName(selectionSet))
        {
            return true;
        }

        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode { SelectionSet: { } childSet } fieldNode
                    when type is IComplexTypeDefinition complexType
                        && complexType.Fields.TryGetField(fieldNode.Name.Value, out var field):
                    if (MissingTypeName(childSet, field.Type.NamedType(), schema))
                    {
                        return true;
                    }

                    break;

                case InlineFragmentNode inlineFragment:
                    var fragmentType =
                        inlineFragment.TypeCondition is { } typeCondition
                        && schema.Types.TryGetType(typeCondition.Name.Value, out var conditionType)
                            ? conditionType
                            : type;

                    if (MissingTypeName(inlineFragment.SelectionSet, fragmentType, schema))
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    private static bool SelectsTypeName(SelectionSetNode selectionSet)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode fieldNode
                && fieldNode.Name.Value == IntrospectionFieldNames.TypeName)
            {
                return true;
            }
        }

        return false;
    }
}
