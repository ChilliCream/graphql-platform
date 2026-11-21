using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// An argument whose <c>@require</c> directive contains a context path must not declare a default
/// value. Such an argument raises a <c>REQUIRE_CONTEXT_DEFAULT_VALUE</c> error.
/// </summary>
internal sealed class RequireContextDefaultValueRule : IEventHandler<FieldArgumentEvent>
{
    public void Handle(FieldArgumentEvent @event, CompositionContext context)
    {
        var (argument, _, _, schema) = @event;

        if (argument.RequireInfo is { ContextNames.Length: > 0 } requireInfo
            && argument.DefaultValue is not null)
        {
            context.Log.Write(RequireContextDefaultValue(requireInfo.Directive, argument, schema));
        }
    }
}
