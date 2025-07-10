using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__implements</c> directive specifies on which source schema an interface is
/// implemented by an object or interface type.
/// </summary>
internal sealed class FusionImplementsMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionImplementsMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition stringType)
        : base(FusionImplements)
    {
        Description = FusionImplementsMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType))
            {
                Description = FusionImplementsMutableDirectiveDefinition_Argument_Schema_Description
            });

        Arguments.Add(new MutableInputFieldDefinition(Interface, new NonNullType(stringType))
        {
            Description = FusionImplementsMutableDirectiveDefinition_Argument_Interface_Description
        });

        IsRepeatable = true;
        Locations = DirectiveLocation.Object | DirectiveLocation.Interface;
    }
}
