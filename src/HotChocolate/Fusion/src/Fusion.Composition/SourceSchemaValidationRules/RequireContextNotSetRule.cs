using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Every context name that the <c>@require</c> directive of an argument references must be the
/// name of an <c>@context</c> directive on a type in the same source schema. Each missing name
/// raises a <c>REQUIRE_CONTEXT_NOT_SET</c> error.
/// </summary>
internal sealed class RequireContextNotSetRule : IEventHandler<FieldArgumentEvent>
{
    public void Handle(FieldArgumentEvent @event, CompositionContext context)
    {
        var (argument, _, _, schema) = @event;

        if (argument.RequireInfo is not { ContextNames.Length: > 0 } requireInfo)
        {
            return;
        }

        var unsetNames = new HashSet<string>(requireInfo.ContextNames);

        foreach (var type in schema.Types)
        {
            foreach (var contextName in requireInfo.ContextNames)
            {
                if (unsetNames.Contains(contextName) && type.SetsContext(contextName))
                {
                    unsetNames.Remove(contextName);
                }
            }

            if (unsetNames.Count == 0)
            {
                return;
            }
        }

        foreach (var contextName in requireInfo.ContextNames)
        {
            if (unsetNames.Contains(contextName))
            {
                context.Log.Write(
                    RequireContextNotSet(requireInfo.Directive, argument, contextName, schema));
            }
        }
    }
}
