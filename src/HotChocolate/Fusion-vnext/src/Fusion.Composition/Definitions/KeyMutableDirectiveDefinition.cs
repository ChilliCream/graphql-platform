using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@key</c> directive is used to designate an entity’s unique key, which identifies how to
/// uniquely reference an instance of an entity across different source schemas. It allows a source
/// schema to indicate which fields form a unique identifier, or <b>key</b>, for an entity.
/// </summary>
internal sealed class KeyMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public KeyMutableDirectiveDefinition(MutableScalarTypeDefinition fieldSelectionSetType)
        : base(WellKnownDirectiveNames.Key)
    {
        Description = KeyMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Fields,
                new NonNullType(fieldSelectionSetType))
            {
                Description = KeyMutableDirectiveDefinition_Argument_Fields_Description
            });

        IsRepeatable = true;

        Locations = DirectiveLocation.Interface | DirectiveLocation.Object;
    }
}
