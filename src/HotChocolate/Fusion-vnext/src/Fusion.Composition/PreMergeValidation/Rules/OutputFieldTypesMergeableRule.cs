using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation.Contracts;
using HotChocolate.Fusion.Results;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// Fields on objects or interfaces that have the same name are considered semantically equivalent
/// and mergeable when they have a mergeable field type.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Output-Field-Types-Mergeable">
/// Specification
/// </seealso>
internal sealed class OutputFieldTypesMergeableRule : IPreMergeValidationRule
{
    public Result Run(PreMergeValidationContext context)
    {
        var loggingSession = context.Log.CreateSession();

        foreach (var outputTypeInfo in context.OutputTypeInfo)
        {
            foreach (var fieldInfo in outputTypeInfo.FieldInfo)
            {
                if (!ValidationHelper.FieldsAreMergeable(fieldInfo.Fields))
                {
                    loggingSession.Write(
                        LogEntryHelper.OutputFieldTypesNotMergeable(
                            fieldInfo.FieldName,
                            outputTypeInfo.TypeName));
                }
            }
        }

        return loggingSession.ErrorCount == 0
            ? Result.Success()
            : ErrorHelper.PreMergeValidationRuleFailed(this);
    }
}
