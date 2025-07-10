using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// Input types are merged by intersection, meaning that the merged input type will have all fields
/// that are present in all input types with the same name. This rule ensures that input object
/// types with the same name across different schemas share a consistent set of required fields.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Input-With-Missing-Required-Fields">
/// Specification
/// </seealso>
internal sealed class InputWithMissingRequiredFieldsRule : IEventHandler<InputTypeGroupEvent>
{
    public void Handle(InputTypeGroupEvent @event, CompositionContext context)
    {
        var (_, inputTypeGroup) = @event;

        var requiredFieldNames =
            inputTypeGroup
                .Where(i => !i.InputType.HasInaccessibleDirective())
                .SelectMany(i => i.InputType.Fields.AsEnumerable())
                .Where(f => !f.HasInaccessibleDirective() && f.Type is NonNullType)
                .Select(f => f.Name)
                .ToImmutableHashSet();

        foreach (var (inputType, schema) in inputTypeGroup)
        {
            if (inputType.HasInaccessibleDirective())
            {
                continue;
            }

            foreach (var requiredFieldName in requiredFieldNames)
            {
                if (!inputType.Fields.ContainsName(requiredFieldName))
                {
                    context.Log.Write(
                        InputWithMissingRequiredFields(requiredFieldName, inputType, schema));
                }
            }
        }
    }
}
