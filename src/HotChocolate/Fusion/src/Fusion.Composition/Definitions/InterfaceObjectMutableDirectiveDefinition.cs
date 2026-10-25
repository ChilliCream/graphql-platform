using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@interfaceObject</c> directive declares an object type that acts as a stand-in for an
/// interface of the same name defined in one or more other source schemas. It lets a source schema
/// contribute fields to that interface without defining any of its implementing types.
/// </summary>
internal sealed class InterfaceObjectMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public InterfaceObjectMutableDirectiveDefinition() : base(WellKnownDirectiveNames.InterfaceObject)
    {
        Description = InterfaceObjectMutableDirectiveDefinition_Description;

        Locations = DirectiveLocation.Object;
    }
}
