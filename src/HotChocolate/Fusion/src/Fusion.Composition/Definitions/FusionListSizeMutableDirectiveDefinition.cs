using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__listSize</c> directive specifies list size metadata for each source schema.
/// </summary>
internal sealed class FusionListSizeMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionListSizeMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition intType,
        MutableScalarTypeDefinition stringType,
        MutableScalarTypeDefinition booleanType)
        : base(FusionListSize)
    {
        Description = FusionListSizeMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType))
            {
                Description = FusionListSizeMutableDirectiveDefinition_Argument_Schema_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(AssumedSize, intType)
            {
                Description = FusionListSizeMutableDirectiveDefinition_Argument_AssumedSize_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(SlicingArguments, new ListType(new NonNullType(stringType)))
            {
                Description = FusionListSizeMutableDirectiveDefinition_Argument_SlicingArguments_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(SlicingArgumentDefaultValue, intType)
            {
                Description = FusionListSizeMutableDirectiveDefinition_Argument_SlicingArgumentDefaultValue_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(SizedFields, new ListType(new NonNullType(stringType)))
            {
                Description = FusionListSizeMutableDirectiveDefinition_Argument_SizedFields_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(RequireOneSlicingArgument, booleanType)
            {
                Description = FusionListSizeMutableDirectiveDefinition_Argument_RequireOneSlicingArgument_Description
            });

        IsRepeatable = true;

        Locations = DirectiveLocation.FieldDefinition;
    }
}
