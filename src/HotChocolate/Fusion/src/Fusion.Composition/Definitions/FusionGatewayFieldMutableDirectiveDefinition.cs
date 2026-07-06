using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__gateway_field</c> directive marks a field that is implemented by the gateway
/// itself rather than resolved from an underlying source schema, such as the global object
/// identification <c>node</c> field.
/// </summary>
internal sealed class FusionGatewayFieldMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionGatewayFieldMutableDirectiveDefinition() : base(FusionGatewayField)
    {
        Description = FusionGatewayFieldMutableDirectiveDefinition_Description;

        Locations = DirectiveLocation.FieldDefinition;
    }
}
