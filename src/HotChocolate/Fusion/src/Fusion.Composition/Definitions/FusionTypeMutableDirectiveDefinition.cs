using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__type</c> directive specifies which source schemas provide parts of a composite
/// type.
/// </summary>
internal sealed class FusionTypeMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionTypeMutableDirectiveDefinition(MutableEnumTypeDefinition schemaMutableEnumType)
        : base(FusionType)
    {
        Description = FusionTypeMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType))
            {
                Description = FusionTypeMutableDirectiveDefinition_Argument_Schema_Description
            });

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
