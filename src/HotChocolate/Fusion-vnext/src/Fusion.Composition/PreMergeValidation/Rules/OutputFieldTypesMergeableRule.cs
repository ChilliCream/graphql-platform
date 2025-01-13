using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// Fields on objects or interfaces that have the same name are considered semantically equivalent
/// and mergeable when they have a mergeable field type.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Output-Field-Types-Mergeable">
/// Specification
/// </seealso>
internal sealed class OutputFieldTypesMergeableRule : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
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
                    OutputFieldTypesNotMergeable(
                        fieldInfoA.Field,
                        typeName,
                        fieldInfoA.Schema,
                        fieldInfoB.Schema));
            }
        }
    }
}
