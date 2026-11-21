using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>field</c> argument of the <c>@is</c> directive must not contain a context path. A
/// context path is only allowed in the <c>field</c> argument of the <c>@require</c> directive. If
/// the selection map contains a context path, the error <c>IS_FIELDS_HAS_CONTEXT_PATH</c> is
/// raised.
/// </summary>
internal sealed class IsFieldsHasContextPathRule : IEventHandler<FieldArgumentEvent>
{
    public void Handle(FieldArgumentEvent @event, CompositionContext context)
    {
        var (argument, _, _, schema) = @event;

        if (argument.IsInfo is { ContextNames.Length: > 0 } isInfo)
        {
            context.Log.Write(IsFieldsHasContextPath(isInfo.Directive, argument, schema));
        }
    }
}
