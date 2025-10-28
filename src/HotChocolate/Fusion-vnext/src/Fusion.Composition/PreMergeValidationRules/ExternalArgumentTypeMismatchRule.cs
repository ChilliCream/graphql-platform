using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// This rule ensures that arguments on fields marked as <c>@external</c> have types compatible with
/// the corresponding arguments on the fields defined in other source schemas. The arguments must
/// have the exact same type signature, including nullability and list nesting.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Argument-Type-Mismatch">
/// Specification
/// </seealso>
internal sealed class ExternalArgumentTypeMismatchRule : IEventHandler<OutputFieldGroupEvent>
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

        var firstNonExternalFieldInfo = nonExternalFieldGroup.FirstOrDefault();

        foreach (var argumentName in argumentNames)
        {
            var nonExternalArgumentType = firstNonExternalFieldInfo!.Field.Arguments[argumentName].Type;

            foreach (var (externalField, _, externalSchema) in externalFieldGroup)
            {
                if (!externalField.Arguments.TryGetField(argumentName, out var externalArgument))
                {
                    continue;
                }

                var externalArgumentType = externalArgument.Type;

                if (!externalArgumentType.Equals(nonExternalArgumentType, TypeComparison.Structural))
                {
                    context.Log.Write(
                        ExternalArgumentTypeMismatch(
                            externalArgument,
                            externalField.Coordinate,
                            externalSchema,
                            firstNonExternalFieldInfo.Schema.Name,
                            nonExternalArgumentType));
                }
            }
        }
    }
}
