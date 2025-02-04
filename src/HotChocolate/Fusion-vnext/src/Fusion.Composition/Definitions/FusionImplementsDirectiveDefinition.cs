using HotChocolate.Skimmed;
using HotChocolate.Types;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionImplementsDirectiveDefinition : DirectiveDefinition
{
    public FusionImplementsDirectiveDefinition(
        EnumTypeDefinition schemaEnumType,
        ScalarTypeDefinition stringType)
        : base(FusionImplements)
    {
        Arguments.Add(new InputFieldDefinition(Schema, new NonNullTypeDefinition(schemaEnumType)));
        Arguments.Add(new InputFieldDefinition(Interface, new NonNullTypeDefinition(stringType)));

        IsRepeatable = true;
        Locations = DirectiveLocation.Object | DirectiveLocation.Interface;
    }
}
