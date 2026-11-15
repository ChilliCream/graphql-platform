using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// Field arguments are merged by intersection. This rule reports a warning for each optional
/// argument that is not defined in every source schema that defines the field, since it is removed
/// from the composite schema. Arguments marked with <c>@require</c> are not counted as defined.
/// </summary>
internal sealed class FieldArgumentNotSharedRule : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var fieldGroup = @event.FieldGroup.Where(i => !i.Field.IsOverridden).ToImmutableArray();

        if (fieldGroup.Length < 2
            || fieldGroup.Any(i => i.Field.HasInaccessibleDirective() || i.Type.HasInaccessibleDirective()))
        {
            return;
        }

        var arguments =
            fieldGroup
                .SelectMany(i => i.Field.Arguments.AsEnumerable())
                .Where(a => !a.HasRequireDirective)
                .ToImmutableArray();

        // Required and inaccessible arguments are excluded, as they are reported by other rules
        // or are not part of the composite schema.
        var excludedArgumentNames =
            arguments
                .Where(a => a.HasInaccessibleDirective() || a.Type is NonNullType)
                .Select(a => a.Name)
                .ToImmutableHashSet();

        var argumentNames =
            arguments
                .Select(a => a.Name)
                .Where(n => !excludedArgumentNames.Contains(n))
                .Distinct()
                .ToImmutableArray();

        foreach (var (field, type, schema) in fieldGroup)
        {
            foreach (var argumentName in argumentNames)
            {
                var hasArgument =
                    field.Arguments
                        .AsEnumerable()
                        .Any(a => a.Name == argumentName && !a.HasRequireDirective);

                if (!hasArgument)
                {
                    context.Log.Write(FieldArgumentNotShared(argumentName, field, type.Name, schema));
                }
            }
        }
    }
}
