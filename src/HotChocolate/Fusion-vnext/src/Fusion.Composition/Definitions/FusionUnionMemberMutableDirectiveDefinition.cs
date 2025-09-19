using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__unionMember</c> directive specifies which source schema provides a member type
/// of a union.
/// </summary>
internal sealed class FusionUnionMemberMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionUnionMemberMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition stringType)
        : base(FusionUnionMember)
    {
        Description = FusionUnionMemberMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullType(schemaMutableEnumType))
            {
                Description =
                    FusionUnionMemberMutableDirectiveDefinition_Argument_Schema_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Member,
                new NonNullType(stringType))
            {
                Description =
                    FusionUnionMemberMutableDirectiveDefinition_Argument_Member_Description
            });

        IsRepeatable = true;
        Locations = DirectiveLocation.Union;
    }
}
