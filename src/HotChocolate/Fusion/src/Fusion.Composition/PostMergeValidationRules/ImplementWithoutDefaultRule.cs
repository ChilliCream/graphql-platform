using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// The <c>@implement</c> directive only has meaning when a default implementation exists to replace.
/// A field marked with <c>@implement</c> for which no applicable default exists, whether because of a
/// typo, a removed default, or a marker placed on a real interface field, fails composition with an
/// <c>IMPLEMENT_WITHOUT_DEFAULT</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Implement-Without-Default">
/// Specification
/// </seealso>
internal sealed class ImplementWithoutDefaultRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var mergedSchema = @event.Schema;
        var schemas = context.SchemaDefinitions;

        foreach (var schema in schemas)
        {
            foreach (var type in schema.Types)
            {
                if (type is not MutableComplexTypeDefinition complexType)
                {
                    continue;
                }

                foreach (var field in complexType.Fields)
                {
                    if (!field.HasImplementDirective)
                    {
                        continue;
                    }

                    if (!HasApplicableDefault(complexType, field.Name, mergedSchema, schemas))
                    {
                        context.Log.Write(ImplementWithoutDefault(field, schema));
                    }
                }
            }
        }
    }

    private static bool HasApplicableDefault(
        MutableComplexTypeDefinition sourceType,
        string fieldName,
        MutableSchemaDefinition mergedSchema,
        IReadOnlyList<MutableSchemaDefinition> schemas)
    {
        // @implement on a real interface field can never replace a default.
        if (sourceType is MutableInterfaceTypeDefinition)
        {
            return false;
        }

        // A stand-in replaces a less specific interface's default; an ordinary implementing type
        // replaces a default contributed to any interface it implements. Both are resolved against
        // the completed implements relation on the merged type (an interface for a stand-in, the
        // object type otherwise).
        if (!mergedSchema.Types.TryGetType(sourceType.Name, out MutableComplexTypeDefinition? mergedType))
        {
            return false;
        }

        foreach (var ancestor in mergedType.Implements)
        {
            if (InterfaceObjectMetadata.DefaultFields(schemas, ancestor.Name).Contains(fieldName))
            {
                return true;
            }
        }

        return false;
    }
}
