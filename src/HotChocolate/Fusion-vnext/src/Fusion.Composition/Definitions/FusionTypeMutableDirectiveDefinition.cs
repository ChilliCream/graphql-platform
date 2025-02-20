using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionTypeMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionTypeMutableDirectiveDefinition(MutableEnumTypeDefinition schemaMutableEnumType) : base(FusionType)
    {
        Arguments.Add(new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType)));

        IsRepeatable = true;

        Locations =
            DirectiveLocation.Enum
            | DirectiveLocation.InputObject
            | DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Scalar
            | DirectiveLocation.Union;
    }
}
