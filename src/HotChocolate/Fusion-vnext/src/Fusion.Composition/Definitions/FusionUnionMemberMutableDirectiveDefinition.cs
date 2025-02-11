using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionUnionMemberMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionUnionMemberMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        ScalarTypeDefinition stringType)
        : base(FusionUnionMember)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullTypeDefinition(schemaMutableEnumType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Member,
                new NonNullTypeDefinition(stringType)));

        IsRepeatable = true;
        Locations = DirectiveLocation.Union;
    }
}
