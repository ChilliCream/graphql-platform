using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Reports <c>@authenticated</c>, <c>@requiresScopes</c> and <c>@policy</c> on a type that is
/// marked with <c>@interfaceObject</c>. The fields of such a type can be annotated.
/// </summary>
internal sealed class AuthorizationOnInterfaceObjectRule : IEventHandler<ComplexTypeEvent>
{
    public void Handle(ComplexTypeEvent @event, CompositionContext context)
    {
        var (complexType, schema) = @event;

        if (complexType is not MutableObjectTypeDefinition standIn
            || !standIn.Directives.ContainsName(DirectiveNames.InterfaceObject))
        {
            return;
        }

        foreach (var directive in standIn.Directives.AsEnumerable())
        {
            if (directive.Name is DirectiveNames.Authenticated
                or DirectiveNames.RequiresScopes
                or DirectiveNames.Policy)
            {
                context.Log.Write(AuthorizationOnInterfaceObject(standIn, directive.Name, schema));
            }
        }
    }
}
