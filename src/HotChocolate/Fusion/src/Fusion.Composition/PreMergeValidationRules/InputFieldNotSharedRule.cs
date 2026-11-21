using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// Input types are merged by intersection. This rule reports a warning for each optional input
/// field that is not defined in every source schema, since it is removed from the composite schema.
/// </summary>
internal sealed class InputFieldNotSharedRule : IEventHandler<InputTypeGroupEvent>
{
    public void Handle(InputTypeGroupEvent @event, CompositionContext context)
    {
        var (_, inputTypeGroup) = @event;

        if (inputTypeGroup.Length < 2 || inputTypeGroup.Any(i => i.InputType.HasInaccessibleDirective()))
        {
            return;
        }

        var fields = inputTypeGroup.SelectMany(i => i.InputType.Fields.AsEnumerable()).ToImmutableArray();

        // Required and inaccessible fields are excluded, as they are reported by other rules
        // or are not part of the composite schema.
        var excludedFieldNames =
            fields
                .Where(f => f.HasInaccessibleDirective() || f.Type is NonNullType)
                .Select(f => f.Name)
                .ToImmutableHashSet();

        var fieldNames =
            fields
                .Select(f => f.Name)
                .Where(n => !excludedFieldNames.Contains(n))
                .Distinct()
                .ToImmutableArray();

        foreach (var (inputType, schema) in inputTypeGroup)
        {
            foreach (var fieldName in fieldNames)
            {
                if (!inputType.Fields.ContainsName(fieldName))
                {
                    context.Log.Write(InputFieldNotShared(fieldName, inputType, schema));
                }
            }
        }
    }
}
