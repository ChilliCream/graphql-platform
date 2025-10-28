using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// This rule ensures that a field marked as <c>@external</c> has a return type compatible with the
/// corresponding field defined in other source schemas. Fields with the same name must represent
/// the same data type to maintain schema consistency.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Type-Mismatch">
/// Specification
/// </seealso>
internal sealed class ExternalTypeMismatchRule : IEventHandler<OutputFieldGroupEvent>
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

        if (nonExternalFieldGroup.IsEmpty)
        {
            return;
        }

        var (field, _, schema) = nonExternalFieldGroup.First();

        foreach (var (externalField, _, externalSchema) in externalFieldGroup)
        {
            if (!externalField.Type.Equals(field.Type, TypeComparison.Structural))
            {
                context.Log.Write(
                    ExternalTypeMismatch(externalField, externalSchema, schema.Name, field.Type));
            }
        }
    }
}
