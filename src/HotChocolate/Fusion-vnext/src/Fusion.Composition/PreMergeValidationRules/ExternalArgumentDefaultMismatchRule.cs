using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

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

        if (!fieldGroup.Any(i => i.Field.HasExternalDirective()))
        {
            return;
        }

        var argumentNames = fieldGroup
            .SelectMany(i => i.Field.Arguments, (_, arg) => arg.Name)
            .ToImmutableHashSet();

        foreach (var argumentName in argumentNames)
        {
            var arguments = fieldGroup
                .SelectMany(i => i.Field.Arguments.Where(a => a.Name == argumentName))
                .ToImmutableArray();

            var defaultValue = arguments[0].DefaultValue;

            foreach (var argument in arguments)
            {
                if (!SyntaxComparer.BySyntax.Equals(argument.DefaultValue, defaultValue))
                {
                    context.Log.Write(
                        ExternalArgumentDefaultMismatch(argumentName, fieldName, typeName));
                }
            }
        }
    }
}
