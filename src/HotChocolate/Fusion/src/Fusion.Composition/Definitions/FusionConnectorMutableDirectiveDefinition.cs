using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using argNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__connector</c> directive is applied to a <c>fusion__Schema</c> enum value to
/// declare which connector kind handles the corresponding source schema. The default GraphQL
/// connector treats absent kinds as <c>"GraphQL"</c>.
/// </summary>
internal sealed class FusionConnectorMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionConnectorMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(FusionConnector)
    {
        Description = FusionConnectorMutableDirectiveDefinition_Description;

        Arguments.Add(new MutableInputFieldDefinition(argNames.Kind, new NonNullType(stringType))
        {
            Description = FusionConnectorMutableDirectiveDefinition_Argument_Kind_Description
        });

        Locations = DirectiveLocation.EnumValue;
    }
}
