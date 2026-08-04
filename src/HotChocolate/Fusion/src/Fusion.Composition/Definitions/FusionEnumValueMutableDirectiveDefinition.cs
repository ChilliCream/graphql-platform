using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__enumValue</c> directive specifies which source schema provides an enum value.
/// </summary>
internal sealed class FusionEnumValueMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionEnumValueMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType)
        : base(FusionEnumValue)
    {
        Description = FusionEnumValueMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullType(schemaMutableEnumType))
            {
                Description = FusionEnumValueMutableDirectiveDefinition_Argument_Schema_Description
            });

        IsRepeatable = true;
        Locations = DirectiveLocation.EnumValue;
    }
}
