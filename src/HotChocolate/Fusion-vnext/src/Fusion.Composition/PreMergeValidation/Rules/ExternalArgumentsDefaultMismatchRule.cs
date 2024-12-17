using HotChocolate.Fusion.Events;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// This rule ensures that arguments on fields marked as @external have default values compatible
/// with the corresponding arguments on fields from other source schemas where the field is defined (non-@external).
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Argument-Default-Mismatch">
/// Specification
/// </seealso>
internal sealed class ExternalArgumentsDefaultMismatchRule
    : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var (fieldName, fieldGroup, typeName) = @event;

        if (fieldGroup.FirstOrDefault(i => ValidationHelper.IsExternal(i.Field)) is { } externalField)
        {
            var argumentNames = fieldGroup.SelectMany(fg => fg.Field.Arguments, (_, arg) => arg.Name).ToHashSet();
            foreach (var argumentName in argumentNames)
            {
                if (!externalField.Field.Arguments.TryGetField(argumentName, out var argumentField))
                {
                    // Logged in separate rule.
                    continue;
                }

                var defaultValue = argumentField.DefaultValue;
                foreach (var currentField in fieldGroup.Except([externalField]))
                {
                    if (!currentField.Field.Arguments.TryGetField(argumentName, out argumentField))
                    {
                        // Logged in separate rule.
                        continue;
                    }

                    var currentValue = argumentField.DefaultValue;
                    var match = (currentValue, defaultValue) switch
                    {
                        (null, null) => true,
                        (not null, null) => false,
                        (null, not null) => false,
                        _ => currentValue.Value!.Equals(defaultValue.Value)
                    };

                    if (!match)
                    {
                        context.Log.Write(ExternalArgumentDefaultMismatch(fieldName, typeName));
                    }
                }
            }
        }
    }
}
