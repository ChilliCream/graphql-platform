using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// This rule ensures that arguments on fields marked as <c>@external</c> have default values
/// compatible with the corresponding arguments on fields from other source schemas where the field
/// is defined (non-<c>@external</c>).
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Argument-Default-Mismatch">
/// Specification
/// </seealso>
internal sealed class ExternalArgumentDefaultMismatchRule : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var (fieldName, fieldGroup, typeName) = @event;

        IReadOnlyCollection<OutputFieldInfo> externalFields =
            [..fieldGroup.Where(i => ValidationHelper.IsExternal(i.Field))];
        if (externalFields.Count == 0)
        {
            return;
        }

        var argumentNames = fieldGroup
            .SelectMany(i => i.Field.Arguments, (_, arg) => arg.Name)
            .ToHashSet();

        foreach (var argumentName in argumentNames)
        {
            IReadOnlyList<InputFieldDefinition> arguments = [..fieldGroup.Select(i => i.Field.Arguments[argumentName])];
            var defaultValue = arguments[0].DefaultValue;

            foreach (var argument in arguments)
            {
                var currentDefaultValue = argument.DefaultValue;
                var match = (currentDefaultValue, defaultValue) switch
                {
                    (null, null) => true,
                    (not null, null) => false,
                    (null, not null) => false,
                    _ => currentDefaultValue.Value!.Equals(defaultValue.Value)
                };

                if (!match)
                {
                    context.Log.Write(ExternalArgumentDefaultMismatch(fieldName, typeName, argumentName));
                }
            }
        }
    }
}
