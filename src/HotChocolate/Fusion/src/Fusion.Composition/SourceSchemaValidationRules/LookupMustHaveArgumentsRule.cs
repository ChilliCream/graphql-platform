using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Fields annotated with the <c>@lookup</c> directive identify a single entity by the arguments
/// supplied to them. A lookup field that declares no arguments has no key with which to resolve an
/// entity and cannot participate in composition. This rule reports such fields as invalid.
/// </summary>
internal sealed class LookupMustHaveArgumentsRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;

        if (field.IsLookup && field.Arguments.Count == 0)
        {
            context.Log.Write(LookupMustHaveArguments(field, schema));
        }
    }
}
