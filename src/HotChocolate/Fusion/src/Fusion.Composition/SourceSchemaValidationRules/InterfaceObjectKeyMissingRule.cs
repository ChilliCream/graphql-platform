using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// An object type annotated with <c>@interfaceObject</c> stands in for an interface defined in one
/// or more other source schemas. The composite schema resolves this stand-in as an independently
/// queryable entity, so the type must declare at least one <c>@key</c>, exactly as any other entity
/// type would. A stand-in with no key cannot be targeted by the distributed executor and cannot
/// contribute fields to the interface it stands in for.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Interface-Object-Key-Missing">
/// Specification
/// </seealso>
internal sealed class InterfaceObjectKeyMissingRule : IEventHandler<ComplexTypeEvent>
{
    public void Handle(ComplexTypeEvent @event, CompositionContext context)
    {
        var (complexType, schema) = @event;

        if (complexType is not MutableObjectTypeDefinition standIn
            || !standIn.Directives.ContainsName(WellKnownDirectiveNames.InterfaceObject))
        {
            return;
        }

        if (!standIn.Directives.ContainsName(WellKnownDirectiveNames.Key))
        {
            context.Log.Write(InterfaceObjectKeyMissing(standIn, schema));
        }
    }
}
