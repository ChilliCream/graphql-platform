using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Reports an <c>@context</c> directive on an object type that is marked with
/// <c>@interfaceObject</c>. Such a usage raises a <c>CONTEXT_ON_INTERFACE_OBJECT</c> error.
/// </summary>
internal sealed class ContextOnInterfaceObjectRule : IEventHandler<ComplexTypeEvent>
{
    public void Handle(ComplexTypeEvent @event, CompositionContext context)
    {
        var (complexType, schema) = @event;

        if (complexType is not MutableObjectTypeDefinition objectType
            || !objectType.Directives.ContainsName(DirectiveNames.InterfaceObject))
        {
            return;
        }

        foreach (var directive in objectType.Directives.AsEnumerable())
        {
            if (directive.Name == DirectiveNames.Context)
            {
                context.Log.Write(ContextOnInterfaceObject(directive, objectType, schema));
            }
        }
    }
}
