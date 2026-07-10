using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionExecutionMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionExecutionMutableDirectiveDefinition(
        MutableEnumTypeDefinition nodeResolutionType)
        : base(WellKnownDirectiveNames.FusionExecution)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.NodeResolution,
                new NonNullType(nodeResolutionType))
            {
                DefaultValue = new EnumValueNode("GATEWAY")
            });

        Locations = DirectiveLocation.Schema;
    }
}
