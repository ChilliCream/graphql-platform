using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// The <c>@policy</c> directive is evaluated against a concrete runtime object, so it can only be
/// declared on an object type or on a field of an object type. Interfaces are abstract: there is
/// no runtime type to evaluate a policy against at the interface level, and implementing types are
/// not required to share the same authorization requirements. Declaring <c>@policy</c> on an
/// interface type or on one of its fields is therefore invalid. An <c>@interfaceObject</c>
/// stand-in represents the interface it stands in for, so <c>@policy</c> on the stand-in type or
/// on one of its fields is invalid for the same reason.
/// </summary>
internal sealed class PolicyOnInterfaceRule
    : IEventHandler<ComplexTypeEvent>
    , IEventHandler<OutputFieldEvent>
{
    public void Handle(ComplexTypeEvent @event, CompositionContext context)
    {
        var (complexType, schema) = @event;

        if ((complexType is MutableInterfaceTypeDefinition || IsInterfaceObjectStandIn(complexType))
            && complexType.Directives.ContainsName(WellKnownDirectiveNames.Policy))
        {
            context.Log.Write(PolicyOnInterfaceType(complexType, schema));
        }
    }

    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if ((type is MutableInterfaceTypeDefinition || IsInterfaceObjectStandIn(type))
            && field.Directives.ContainsName(WellKnownDirectiveNames.Policy))
        {
            context.Log.Write(PolicyOnInterfaceField(field, schema));
        }
    }

    private static bool IsInterfaceObjectStandIn(ITypeDefinition type)
        => type is MutableObjectTypeDefinition objectType
            && objectType.Directives.ContainsName(WellKnownDirectiveNames.InterfaceObject);
}
