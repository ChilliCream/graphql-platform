using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionLookupMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionLookupMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition fieldSelectionSetType,
        MutableScalarTypeDefinition fieldDefinitionType,
        MutableScalarTypeDefinition fieldSelectionMapType,
        MutableScalarTypeDefinition fieldSelectionPathType)
        : base(FusionLookup)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Schema,
                new NonNullType(schemaMutableEnumType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Key,
                new NonNullType(fieldSelectionSetType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Field,
                new NonNullType(fieldDefinitionType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Map,
                new NonNullType(
                    new ListType(new NonNullType(fieldSelectionMapType)))));

        Arguments.Add(new MutableInputFieldDefinition(ArgumentNames.Path, fieldSelectionPathType));

        IsRepeatable = true;

        Locations =
            DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Union;
    }
}
