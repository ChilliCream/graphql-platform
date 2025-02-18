using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// For input object types defined across multiple source schemas, the merged input object type is
/// the intersection of all fields defined in these source schemas. Any field marked with the
/// <c>@inaccessible</c> directive in any source schema is hidden and not included in the merged
/// input object type. An input object type with no fields, after considering <c>@inaccessible</c>
/// annotations, is considered empty and invalid.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Empty-Merged-Input-Object-Type">
/// Specification
/// </seealso>
internal sealed class EmptyMergedInputObjectTypeRule : IEventHandler<InputTypeEvent>
{
    public void Handle(InputTypeEvent @event, CompositionContext context)
    {
        var (inputType, schema) = @event;

        if (inputType.HasFusionInaccessibleDirective())
        {
            return;
        }

        var accessibleFields = inputType.Fields.Where(f => !f.HasFusionInaccessibleDirective());

        if (!accessibleFields.Any())
        {
            context.Log.Write(EmptyMergedInputObjectType(inputType, schema));
        }
    }
}
