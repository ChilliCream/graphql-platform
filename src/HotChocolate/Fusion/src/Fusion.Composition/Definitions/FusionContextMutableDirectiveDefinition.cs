using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__context</c> directive specifies that the values of a type set a named context
/// in a source schema.
/// </summary>
internal sealed class FusionContextMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionContextMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition stringType)
        : base(FusionContext)
    {
        Description = FusionContextMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType))
            {
                Description = FusionContextMutableDirectiveDefinition_Argument_Schema_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Name,
                new NonNullType(stringType))
            {
                Description = FusionContextMutableDirectiveDefinition_Argument_Name_Description
            });

        IsRepeatable = true;
        Locations = DirectiveLocation.Object | DirectiveLocation.Interface | DirectiveLocation.Union;
    }
}
