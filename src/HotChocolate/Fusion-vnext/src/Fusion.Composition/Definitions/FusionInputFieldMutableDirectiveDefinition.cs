using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__inputField</c> directive specifies which source schema provides an input field
/// in a composite input type.
/// </summary>
internal sealed class FusionInputFieldMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionInputFieldMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition stringType)
        : base(FusionInputField)
    {
        Description = FusionInputFieldMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullType(schemaMutableEnumType))
            {
                Description = FusionInputFieldMutableDirectiveDefinition_Argument_Schema_Description
            });

        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.SourceType, stringType)
        {
            Description = FusionInputFieldMutableDirectiveDefinition_Argument_SourceType_Description
        });

        IsRepeatable = true;
        Locations = DirectiveLocation.ArgumentDefinition | DirectiveLocation.InputFieldDefinition;
    }
}
