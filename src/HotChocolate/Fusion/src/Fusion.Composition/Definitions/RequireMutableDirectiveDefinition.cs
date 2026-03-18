using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@require</c> directive is used to express data requirements with other source schemas.
/// Arguments annotated with the <c>@require</c> directive are removed from the <i>composite
/// schema</i> and the value for these will be resolved by the <i>distributed executor</i>.
/// </summary>
internal sealed class RequireMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public RequireMutableDirectiveDefinition(MutableScalarTypeDefinition fieldSelectionMapType)
        : base(WellKnownDirectiveNames.Require)
    {
        Description = RequireMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Field,
                new NonNullType(fieldSelectionMapType))
            {
                Description = RequireMutableDirectiveDefinition_Argument_Field_Description
            });

        Locations = DirectiveLocation.ArgumentDefinition;
    }
}
