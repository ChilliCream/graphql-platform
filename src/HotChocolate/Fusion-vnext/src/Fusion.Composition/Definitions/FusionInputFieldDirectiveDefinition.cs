using HotChocolate.Skimmed;
using HotChocolate.Types;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionInputFieldDirectiveDefinition : DirectiveDefinition
{
    public FusionInputFieldDirectiveDefinition(
        EnumTypeDefinition schemaEnumType,
        ScalarTypeDefinition stringType)
        : base(FusionInputField)
    {
        Arguments.Add(
            new InputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullTypeDefinition(schemaEnumType)));

        Arguments.Add(new InputFieldDefinition(WellKnownArgumentNames.SourceType, stringType));

        IsRepeatable = true;
        Locations = DirectiveLocation.ArgumentDefinition | DirectiveLocation.InputFieldDefinition;
    }
}
