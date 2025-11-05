using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// If one input type is annotated with the @oneOf directive, all input types
/// of the same name must be annotated with it as well.
/// </summary>
internal sealed class InputWithMissingOneOfRule : IEventHandler<InputTypeGroupEvent>
{
    public void Handle(InputTypeGroupEvent @event, CompositionContext context)
    {
        var (_, inputTypeGroup) = @event;

        var firstInputWithOneOf = inputTypeGroup.FirstOrDefault(i => i.InputType.IsOneOf);

        if (firstInputWithOneOf is null || inputTypeGroup.Length == 1)
        {
            return;
        }

        foreach (var (inputType, schema) in inputTypeGroup)
        {
            if (inputType.HasInaccessibleDirective())
            {
                continue;
            }

            if (!inputType.IsOneOf)
            {
                context.Log.Write(
                    LogEntryHelper.InputWithMissingOneOf(
                        inputType,
                        schema,
                        firstInputWithOneOf.Schema));
            }
        }
    }
}
