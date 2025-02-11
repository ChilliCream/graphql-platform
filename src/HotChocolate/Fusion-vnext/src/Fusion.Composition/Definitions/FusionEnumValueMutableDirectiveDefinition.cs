using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionEnumValueMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionEnumValueMutableDirectiveDefinition(MutableEnumTypeDefinition schemaMutableEnumType)
        : base(FusionEnumValue)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullType(schemaMutableEnumType)));

        IsRepeatable = true;
        Locations = DirectiveLocation.EnumValue;
    }
}
