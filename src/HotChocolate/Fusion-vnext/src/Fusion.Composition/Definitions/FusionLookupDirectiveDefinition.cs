using HotChocolate.Skimmed;
using HotChocolate.Types;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionLookupDirectiveDefinition : DirectiveDefinition
{
    public FusionLookupDirectiveDefinition(
        EnumTypeDefinition schemaEnumType,
        ScalarTypeDefinition fieldSelectionSetType,
        ScalarTypeDefinition fieldDefinitionType,
        ScalarTypeDefinition fieldSelectionMapType,
        ScalarTypeDefinition fieldSelectionPathType)
        : base(FusionLookup)
    {
        Arguments.Add(
            new InputFieldDefinition(
                ArgumentNames.Schema,
                new NonNullTypeDefinition(schemaEnumType)));

        Arguments.Add(
            new InputFieldDefinition(
                ArgumentNames.Key,
                new NonNullTypeDefinition(fieldSelectionSetType)));

        Arguments.Add(
            new InputFieldDefinition(
                ArgumentNames.Field,
                new NonNullTypeDefinition(fieldDefinitionType)));

        Arguments.Add(
            new InputFieldDefinition(
                ArgumentNames.Map,
                new NonNullTypeDefinition(
                    new ListTypeDefinition(new NonNullTypeDefinition(fieldSelectionMapType)))));

        Arguments.Add(new InputFieldDefinition(ArgumentNames.Path, fieldSelectionPathType));

        IsRepeatable = true;

        Locations =
            DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Union;
    }
}
