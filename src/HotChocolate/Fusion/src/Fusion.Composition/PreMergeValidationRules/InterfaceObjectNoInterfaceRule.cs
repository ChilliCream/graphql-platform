using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// A stand-in binds to the interface of the same name. At least one source schema must define that
/// name as an interface. If every source schema that declares the type name uses
/// <c>@interfaceObject</c>, and none defines it as an interface, the stand-in has no interface to
/// bind to and composition fails with an <c>INTERFACE_OBJECT_NO_INTERFACE</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Interface-Object-No-Interface">
/// Specification
/// </seealso>
internal sealed class InterfaceObjectNoInterfaceRule : IEventHandler<TypeGroupEvent>
{
    public void Handle(TypeGroupEvent @event, CompositionContext context)
    {
        var (typeName, typeGroup) = @event;

        var hasStandIn = false;
        var firstStandInSchema = default(MutableSchemaDefinition);

        foreach (var typeInfo in typeGroup)
        {
            if (typeInfo.Type is MutableObjectTypeDefinition o
                && o.Directives.ContainsName(WellKnownDirectiveNames.InterfaceObject))
            {
                hasStandIn = true;
                firstStandInSchema ??= typeInfo.Schema;
            }
        }

        if (!hasStandIn)
        {
            return;
        }

        var hasInterface = typeGroup.Any(i => i.Type is MutableInterfaceTypeDefinition);

        if (!hasInterface)
        {
            context.Log.Write(InterfaceObjectNoInterface(typeName, firstStandInSchema!));
        }
    }
}
