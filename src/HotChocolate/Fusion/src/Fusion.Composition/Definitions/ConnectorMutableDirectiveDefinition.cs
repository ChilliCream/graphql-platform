using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using argNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__connector</c> directive is applied to a source schema definition to declare
/// which connector kind handles that source schema. The composer lifts the directive onto the
/// corresponding <c>fusion__Schema</c> enum value in the merged execution schema.
/// </summary>
internal sealed class ConnectorMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public ConnectorMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(FusionConnector)
    {
        Description = ConnectorMutableDirectiveDefinition_Description;

        Arguments.Add(new MutableInputFieldDefinition(argNames.Kind, new NonNullType(stringType))
        {
            Description = ConnectorMutableDirectiveDefinition_Argument_Kind_Description
        });

        Locations = DirectiveLocation.Schema;
    }
}
