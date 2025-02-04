using HotChocolate.Skimmed;
using HotChocolate.Types;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionUnionMemberDirectiveDefinition : DirectiveDefinition
{
    public FusionUnionMemberDirectiveDefinition(
        EnumTypeDefinition schemaEnumType,
        ScalarTypeDefinition stringType)
        : base(FusionUnionMember)
    {
        Arguments.Add(
            new InputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullTypeDefinition(schemaEnumType)));

        Arguments.Add(
            new InputFieldDefinition(
                WellKnownArgumentNames.Member,
                new NonNullTypeDefinition(stringType)));

        IsRepeatable = true;
        Locations = DirectiveLocation.Union;
    }
}
