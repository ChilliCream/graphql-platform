using HotChocolate.Types.Mutable;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionCostOptionsMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionCostOptionsMutableDirectiveDefinition(MutableScalarTypeDefinition intType)
        : base(WellKnownDirectiveNames.FusionCostOptions)
    {
        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.DefaultListSize, intType));

        Locations = DirectiveLocation.Schema;
    }
}
