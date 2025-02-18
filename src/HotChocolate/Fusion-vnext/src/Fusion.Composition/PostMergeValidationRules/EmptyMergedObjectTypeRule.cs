using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// For object types defined across multiple source schemas, the merged object type is the superset
/// of all fields defined in these source schemas. However, any field marked with
/// <c>@inaccessible</c> in any source schema is hidden and not included in the merged object type.
/// An object type with no fields, after considering <c>@inaccessible</c> annotations, is considered
/// empty and invalid.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Empty-Merged-Object-Type">
/// Specification
/// </seealso>
internal sealed class EmptyMergedObjectTypeRule : IEventHandler<ObjectTypeEvent>
{
    public void Handle(ObjectTypeEvent @event, CompositionContext context)
    {
        var (objectType, schema) = @event;

        if (schema.IsRootOperationType(objectType) || objectType.HasFusionInaccessibleDirective())
        {
            return;
        }

        var accessibleFields = objectType.Fields.Where(f => !f.HasFusionInaccessibleDirective());

        if (!accessibleFields.Any())
        {
            context.Log.Write(EmptyMergedObjectType(objectType, schema));
        }
    }
}
