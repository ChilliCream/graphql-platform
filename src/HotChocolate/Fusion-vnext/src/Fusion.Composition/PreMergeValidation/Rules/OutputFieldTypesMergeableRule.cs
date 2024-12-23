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
        var (fieldName, fieldGroup, typeName) = @event;

        if (!ValidationHelper.FieldsAreMergeable([.. fieldGroup.Select(i => i.Field)]))
        {
            context.Log.Write(OutputFieldTypesNotMergeable(fieldName, typeName));
        }
    }
}
