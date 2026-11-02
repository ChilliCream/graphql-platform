using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__lookup</c> directive specifies how the distributed executor can resolve data for
/// an entity type from a source schema by a stable key.
/// </summary>
internal sealed class FusionLookupMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionLookupMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition fieldSelectionSetType,
        MutableScalarTypeDefinition fieldDefinitionType,
        MutableScalarTypeDefinition fieldSelectionMapType,
        MutableScalarTypeDefinition fieldSelectionPathType,
        MutableScalarTypeDefinition booleanType)
        : base(FusionLookup)
    {
        Description = FusionLookupMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Schema,
                new NonNullType(schemaMutableEnumType))
            {
                Description = FusionLookupMutableDirectiveDefinition_Argument_Schema_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Key,
                new NonNullType(fieldSelectionSetType))
            {
                Description = FusionLookupMutableDirectiveDefinition_Argument_Key_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Field,
                new NonNullType(fieldDefinitionType))
            {
                Description = FusionLookupMutableDirectiveDefinition_Argument_Field_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Map,
                new NonNullType(
                    new ListType(new NonNullType(fieldSelectionMapType))))
            {
                Description = FusionLookupMutableDirectiveDefinition_Argument_Map_Description
            });

        Arguments.Add(new MutableInputFieldDefinition(ArgumentNames.Path, fieldSelectionPathType)
        {
            Description = FusionLookupMutableDirectiveDefinition_Argument_Path_Description
        });

        Arguments.Add(new MutableInputFieldDefinition(ArgumentNames.Internal, new NonNullType(booleanType))
        {
            DefaultValue = new BooleanValueNode(false),
            Description = FusionLookupMutableDirectiveDefinition_Argument_Internal_Description
        });

        IsRepeatable = true;

        Locations =
            DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Union;
    }
}
