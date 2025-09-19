using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using argNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__schema_metadata</c> directive is used to provide additional metadata for a
/// source schema.
/// </summary>
internal sealed class FusionSchemaMetadataMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionSchemaMetadataMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(FusionSchemaMetadata)
    {
        Description = FusionSchemaMetadataMutableDirectiveDefinition_Description;

        Arguments.Add(new MutableInputFieldDefinition(argNames.Name, new NonNullType(stringType))
        {
            Description = FusionSchemaMetadataMutableDirectiveDefinition_Argument_Name_Description
        });

        Locations = DirectiveLocation.EnumValue;
    }
}
