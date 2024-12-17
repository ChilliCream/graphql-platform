using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// This rule ensures that any field marked as <c>@external</c> in a source schema is actually
/// defined (non-<c>@external</c>) in at least one other source schema. The <c>@external</c>
/// directive is used to indicate that the field is not usually resolved by the source schema it is
/// declared in, implying it should be resolvable by at least one other source schema.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Missing-on-Base">
/// Specification
/// </seealso>
internal sealed class ExternalMissingOnBaseRule : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var (fieldName, fieldGroup, typeName) = @event;

        var externalFieldCount = fieldGroup.Count(i => ValidationHelper.IsExternal(i.Field));
        var nonExternalFieldCount = fieldGroup.Length - externalFieldCount;

        if (externalFieldCount != 0 && nonExternalFieldCount == 0)
        {
            context.Log.Write(ExternalMissingOnBase(fieldName, typeName));
        }
    }
}
