using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// A field that replaces a default implementation contributed by an <c>@interfaceObject</c> stand-in
/// must be marked with <c>@implement</c>. This applies to a field declared directly on an
/// implementing object type, and to a field declared on a more specific interface's own stand-in
/// that collides with a less specific interface's default. An unmarked collision fails composition
/// with an <c>INTERFACE_OBJECT_FIELD_REQUIRES_IMPLEMENT</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Interface-Object-Field-Requires-Implement">
/// Specification
/// </seealso>
internal sealed class InterfaceObjectFieldRequiresImplementRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var mergedSchema = @event.Schema;
        var schemas = context.SchemaDefinitions;

        // Aggregate per interface + field so the same default collision is not reported once per
        // implementing type.
        var reported = new HashSet<(string Interface, string Field)>();

        foreach (var type in mergedSchema.Types)
        {
            switch (type)
            {
                case MutableObjectTypeDefinition objectType:
                    CheckImplementingType(objectType, objectType, schemas, reported, context);
                    break;

                case MutableInterfaceTypeDefinition interfaceType:
                    CheckMoreSpecificStandIn(interfaceType, schemas, reported, context);
                    break;
            }
        }
    }

    private static void CheckImplementingType(
        MutableComplexTypeDefinition mergedType,
        MutableObjectTypeDefinition objectType,
        IReadOnlyList<MutableSchemaDefinition> schemas,
        HashSet<(string, string)> reported,
        CompositionContext context)
    {
        // fieldName -> the interface supplying the default (first match wins for the diagnostic).
        var defaultToInterface = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var interfaceType in mergedType.Implements)
        {
            foreach (var fieldName in InterfaceObjectMetadata.DefaultFields(schemas, interfaceType.Name))
            {
                defaultToInterface.TryAdd(fieldName, interfaceType.Name);
            }
        }

        if (defaultToInterface.Count == 0)
        {
            return;
        }

        foreach (var schema in schemas)
        {
            if (!schema.Types.TryGetType(objectType.Name, out MutableObjectTypeDefinition? sourceType))
            {
                continue;
            }

            foreach (var field in sourceType.Fields)
            {
                if (field.IsExternal
                    || field.IsInternal
                    || field.IsOverridden
                    || field.HasImplementDirective)
                {
                    continue;
                }

                if (defaultToInterface.TryGetValue(field.Name, out var interfaceName)
                    && reported.Add((interfaceName, field.Name)))
                {
                    context.Log.Write(
                        InterfaceObjectFieldRequiresImplement(interfaceName, field.Name, schema));
                }
            }
        }
    }

    private static void CheckMoreSpecificStandIn(
        MutableInterfaceTypeDefinition interfaceType,
        IReadOnlyList<MutableSchemaDefinition> schemas,
        HashSet<(string, string)> reported,
        CompositionContext context)
    {
        // A more specific interface's own stand-in field that collides with a less specific
        // interface's default must carry @implement.
        var ownDefaults = InterfaceObjectMetadata.DefaultFields(schemas, interfaceType.Name);

        if (ownDefaults.Count == 0)
        {
            return;
        }

        var ancestorDefaults = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var ancestor in interfaceType.Implements)
        {
            foreach (var fieldName in InterfaceObjectMetadata.DefaultFields(schemas, ancestor.Name))
            {
                ancestorDefaults.TryAdd(fieldName, ancestor.Name);
            }
        }

        foreach (var (standIn, schema) in InterfaceObjectMetadata.GetStandIns(schemas, interfaceType.Name))
        {
            foreach (var field in standIn.Fields)
            {
                if (!ownDefaults.Contains(field.Name)
                    || field.HasImplementDirective
                    || !ancestorDefaults.TryGetValue(field.Name, out var ancestorName))
                {
                    continue;
                }

                if (reported.Add((ancestorName, field.Name)))
                {
                    context.Log.Write(
                        InterfaceObjectFieldRequiresImplement(ancestorName, field.Name, schema));
                }
            }
        }
    }
}
