using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// <para>
/// The input fields of input objects with the same name must be mergeable. This rule ensures that
/// input objects with the same name in different source schemas have fields that can be merged
/// consistently without conflicts.
/// </para>
/// <para>
/// Input fields are considered mergeable when they share the same name and have compatible types.
/// The compatibility of types is determined by their structure (e.g., lists), excluding
/// nullability. Mergeable input fields with different nullability are considered mergeable,
/// and the resulting merged field will be the most permissive of the two.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Input-Field-Types-mergeable">
/// Specification
/// </seealso>
internal sealed class InputFieldTypesMergeableRule : IEventHandler<InputFieldGroupEvent>
{
    public void Handle(InputFieldGroupEvent @event, CompositionContext context)
    {
        var (_, fieldGroup, typeName) = @event;

        for (var i = 0; i < fieldGroup.Length - 1; i++)
        {
            var fieldInfoA = fieldGroup[i];
            var fieldInfoB = fieldGroup[i + 1];
            var typeA = fieldInfoA.Field.Type;
            var typeB = fieldInfoB.Field.Type;

            if (!ValidationHelper.SameTypeShape(typeA, typeB))
            {
                context.Log.Write(
                    InputFieldTypesNotMergeable(
                        fieldInfoA.Field,
                        typeName,
                        fieldInfoA.Schema,
                        fieldInfoB.Schema));
            }
        }
    }
}
