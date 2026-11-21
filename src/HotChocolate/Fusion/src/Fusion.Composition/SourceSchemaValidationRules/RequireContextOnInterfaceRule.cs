using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// An argument whose <c>@require</c> directive contains a context path must not belong to an
/// interface field or to an object field that implements an interface field of the same name.
/// Such an argument raises a <c>REQUIRE_CONTEXT_ON_INTERFACE</c> error.
/// </summary>
internal sealed class RequireContextOnInterfaceRule : IEventHandler<FieldArgumentEvent>
{
    public void Handle(FieldArgumentEvent @event, CompositionContext context)
    {
        var (argument, field, type, schema) = @event;

        if (argument.RequireInfo is not { ContextNames.Length: > 0 } requireInfo)
        {
            return;
        }

        if (type is MutableInterfaceTypeDefinition
            || (type is MutableObjectTypeDefinition objectType
                && ImplementsInterfaceField(objectType, field)))
        {
            context.Log.Write(RequireContextOnInterface(requireInfo.Directive, argument, schema));
        }
    }

    private static bool ImplementsInterfaceField(
        MutableObjectTypeDefinition objectType,
        MutableOutputFieldDefinition field)
    {
        foreach (var interfaceType in objectType.Implements)
        {
            if (interfaceType.Fields.ContainsName(field.Name))
            {
                return true;
            }
        }

        return false;
    }
}
