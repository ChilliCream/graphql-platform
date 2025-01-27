using HotChocolate.Language;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionFieldDirectiveDefinition : DirectiveDefinition
{
    public FusionFieldDirectiveDefinition(
        EnumTypeDefinition schemaEnumType,
        ScalarTypeDefinition stringType,
        ScalarTypeDefinition fieldSelectionSetType,
        ScalarTypeDefinition booleanType) : base(FusionField)
    {
        Arguments.Add(
            new InputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullTypeDefinition(schemaEnumType)));

        Arguments.Add(new InputFieldDefinition(WellKnownArgumentNames.SourceType, stringType));

        Arguments.Add(
            new InputFieldDefinition(WellKnownArgumentNames.Provides, fieldSelectionSetType));

        Arguments.Add(
            new InputFieldDefinition(
                WellKnownArgumentNames.External,
                new NonNullTypeDefinition(booleanType))
                    { DefaultValue = new BooleanValueNode(false) });

        IsRepeatable = true;
        Locations = DirectiveLocation.FieldDefinition;
    }
}
