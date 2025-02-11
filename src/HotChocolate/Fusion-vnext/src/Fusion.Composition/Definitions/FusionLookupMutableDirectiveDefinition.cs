using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionLookupMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionLookupMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        ScalarTypeDefinition fieldSelectionSetType,
        ScalarTypeDefinition fieldDefinitionType,
        ScalarTypeDefinition fieldSelectionMapType,
        ScalarTypeDefinition fieldSelectionPathType)
        : base(FusionLookup)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Schema,
                new NonNullTypeDefinition(schemaMutableEnumType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Key,
                new NonNullTypeDefinition(fieldSelectionSetType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Field,
                new NonNullTypeDefinition(fieldDefinitionType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Map,
                new NonNullTypeDefinition(
                    new ListTypeDefinition(new NonNullTypeDefinition(fieldSelectionMapType)))));

        Arguments.Add(new MutableInputFieldDefinition(ArgumentNames.Path, fieldSelectionPathType));

        IsRepeatable = true;

        Locations =
            DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Union;
    }
}
