using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// When using the <c>@is</c> directive, the field declaring the argument must be a lookup field
/// (i.e. have the <c>@lookup</c> directive applied).
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Is-Invalid-Usage">
/// Specification
/// </seealso>
internal sealed class IsInvalidUsageRule : IEventHandler<FieldArgumentEvent>
{
    public void Handle(FieldArgumentEvent @event, CompositionContext context)
    {
        var (argument, field, _, schema) = @event;

        var isDirective = argument.IsInfo?.Directive;

        if (isDirective is not null && !field.IsLookup)
        {
            context.Log.Write(IsInvalidUsage(isDirective, argument, schema));
        }
    }
}
