using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@override</c> directive is used to migrate a field from one source schema to another.
/// When a field in the local schema is annotated with <c>@override(from: "Catalog")</c>, it signals
/// that the local schema overrides the field previously contributed by the <c>Catalog</c> source
/// schema. As a result, the composite schema will source this field from the local schema, rather
/// than from the original source schema.
/// </summary>
internal sealed class OverrideMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public OverrideMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(WellKnownDirectiveNames.Override)
    {
        Description = OverrideMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.From,
                new NonNullType(stringType))
            {
                Description = OverrideMutableDirectiveDefinition_Argument_From_Description
            });

        Locations = DirectiveLocation.FieldDefinition;
    }
}
