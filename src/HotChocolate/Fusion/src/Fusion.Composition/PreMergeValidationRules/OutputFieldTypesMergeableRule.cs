using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// Fields on objects or interfaces that have the same name are considered semantically equivalent
/// and mergeable when <see cref="TypeMergeHelper.TryGetLeastRestrictiveType"/> can select a return type for the
/// composed field. This selection considers all field types together and must not depend on source schema order.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Output-Field-Types-Mergeable">
/// Specification
/// </seealso>
internal sealed class OutputFieldTypesMergeableRule : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var fieldGroup = @event.FieldGroup;
        var fieldTypes = new (IType Type, MutableSchemaDefinition Schema)[fieldGroup.Length];

        for (var i = 0; i < fieldGroup.Length; i++)
        {
            fieldTypes[i] = (fieldGroup[i].Field.Type, fieldGroup[i].Schema);
        }

        if (TypeMergeHelper.TryGetLeastRestrictiveType(fieldTypes, out _))
        {
            return;
        }

        // Report the first field whose type cannot be unified with the preceding fields.
        var offendingIndex = fieldGroup.Length - 1;

        for (var i = 1; i < fieldGroup.Length; i++)
        {
            if (!TypeMergeHelper.TryGetLeastRestrictiveType(fieldTypes[..(i + 1)], out _))
            {
                offendingIndex = i;

                break;
            }
        }

        context.Log.Write(
            OutputFieldTypesNotMergeable(
                fieldGroup[0].Field,
                fieldGroup[0].Schema,
                fieldGroup[offendingIndex].Schema));
    }
}
