using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@provides</c> directive indicates that a field can provide certain subfields of its
/// return type from the same source schema, without requiring an additional resolution step
/// elsewhere.
/// </summary>
internal sealed class ProvidesMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public ProvidesMutableDirectiveDefinition(MutableScalarTypeDefinition fieldSelectionSetType)
        : base(WellKnownDirectiveNames.Provides)
    {
        Description = ProvidesMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Fields,
                new NonNullType(fieldSelectionSetType))
            {
                Description = ProvidesMutableDirectiveDefinition_Argument_Fields_Description
            });

        Locations = DirectiveLocation.FieldDefinition;
    }
}
