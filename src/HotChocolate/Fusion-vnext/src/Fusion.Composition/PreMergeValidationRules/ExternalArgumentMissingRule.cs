using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// This rule ensures that fields marked with <c>@external</c> have all the necessary arguments that
/// exist on the corresponding field definitions in other source schemas. Each argument defined on
/// the base field (the field definition in the defining source schema) must be present on the
/// <c>@external</c> field in other source schemas. If an argument is missing on an <c>@external</c>
/// field, the field cannot be resolved correctly, which is an inconsistency.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Argument-Missing">
/// Specification
/// </seealso>
internal sealed class ExternalArgumentMissingRule : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var (_, fieldGroup, _) = @event;

        var externalFieldGroup =
            fieldGroup.Where(i => i.Field.HasExternalDirective()).ToImmutableHashSet();

        if (externalFieldGroup.IsEmpty)
        {
            return;
        }

        var nonExternalFieldGroup =
            fieldGroup.Where(i => !i.Field.HasExternalDirective()).ToImmutableHashSet();

        var argumentNames = nonExternalFieldGroup
            .SelectMany(i => i.Field.Arguments.AsEnumerable(), (_, arg) => arg.Name)
            .ToImmutableHashSet();

        foreach (var argumentName in argumentNames)
        {
            foreach (var (externalField, _, externalSchema) in externalFieldGroup)
            {
                if (!externalField.Arguments.ContainsName(argumentName))
                {
                    context.Log.Write(
                        ExternalArgumentMissing(externalField, externalSchema, argumentName));
                }
            }
        }
    }
}
