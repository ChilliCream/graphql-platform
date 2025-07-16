using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@is</c> directive is utilized on lookup fields to describe how the arguments can be
/// mapped from the entity type that the lookup field resolves. The mapping establishes semantic
/// equivalence between disparate type system members across source schemas and is used in cases
/// where the argument does not 1:1 align with a field on the entity type.
/// </summary>
internal sealed class IsMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public IsMutableDirectiveDefinition(MutableScalarTypeDefinition fieldSelectionMapType)
        : base(WellKnownDirectiveNames.Is)
    {
        Description = IsMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Field,
                new NonNullType(fieldSelectionMapType))
            {
                Description = IsMutableDirectiveDefinition_Argument_Field_Description
            });

        Locations = DirectiveLocation.ArgumentDefinition;
    }
}
