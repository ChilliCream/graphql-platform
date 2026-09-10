using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__interfaceObject</c> directive specifies the source schemas that expose an
/// interface as an <c>@interfaceObject</c> stand-in. Values of the interface produced by such a
/// schema are opaque: the schema holds no authoritative concrete type for them.
/// </summary>
internal sealed class FusionInterfaceObjectMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionInterfaceObjectMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType)
        : base(FusionInterfaceObject)
    {
        Description = FusionInterfaceObjectMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType))
            {
                Description = FusionInterfaceObjectMutableDirectiveDefinition_Argument_Schema_Description
            });

        IsRepeatable = true;
        Locations = DirectiveLocation.Interface;
    }
}
