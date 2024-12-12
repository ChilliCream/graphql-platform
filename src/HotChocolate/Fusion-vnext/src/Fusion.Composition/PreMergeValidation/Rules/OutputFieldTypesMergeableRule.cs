using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation.Contracts;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// Fields on objects or interfaces that have the same name are considered semantically equivalent
/// and mergeable when they have a mergeable field type.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Output-Field-Types-Mergeable">
/// Specification
/// </seealso>
internal sealed class OutputFieldTypesMergeableRule : PreMergeValidationRule
{
    public override void OnEachOutputFieldName(EachOutputFieldNameEvent @event)
    {
        var (context, fieldName, fieldInfo, typeName) = @event;

        if (!ValidationHelper.FieldsAreMergeable([.. fieldInfo.Select(i => i.Field)]))
        {
            context.Log.Write(LogEntryHelper.OutputFieldTypesNotMergeable(fieldName, typeName));
        }
    }
}
