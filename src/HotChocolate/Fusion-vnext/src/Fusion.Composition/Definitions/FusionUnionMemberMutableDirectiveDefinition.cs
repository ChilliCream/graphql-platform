using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionUnionMemberMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionUnionMemberMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition stringType)
        : base(FusionUnionMember)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullType(schemaMutableEnumType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Member,
                new NonNullType(stringType)));

        IsRepeatable = true;
        Locations = DirectiveLocation.Union;
    }
}
