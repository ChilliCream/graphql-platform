using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// When using the <c>@is</c> directive, the field declaring the argument must be a lookup field
/// (i.e. have the <c>@lookup</c> directive applied).
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Is-Invalid-Usage">
/// Specification
/// </seealso>
internal sealed class IsInvalidUsageRule : IEventHandler<IsDirectiveEvent>
{
    public void Handle(IsDirectiveEvent @event, CompositionContext context)
    {
        var (isDirective, argument, field, type, schema) = @event;

        if (!field.Directives.ContainsName(Lookup))
        {
            context.Log.Write(
                IsInvalidUsage(
                    isDirective,
                    argument.Name,
                    field.Name,
                    type.Name,
                    schema));
        }
    }
}
