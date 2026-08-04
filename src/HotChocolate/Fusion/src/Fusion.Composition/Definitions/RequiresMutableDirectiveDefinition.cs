using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@requires</c> directive from Apollo Federation expresses that a field depends on a
/// selection of fields resolved by another source schema. It is recognized on source schemas and
/// rewritten to <c>@require</c> during composition.
/// </summary>
internal sealed class RequiresMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public RequiresMutableDirectiveDefinition(MutableScalarTypeDefinition fieldSelectionSetType)
        : base(FederationDirectiveNames.Requires)
    {
        Description = RequiresMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Fields,
                new NonNullType(fieldSelectionSetType))
            {
                Description = RequiresMutableDirectiveDefinition_Argument_Fields_Description
            });

        Locations = DirectiveLocation.FieldDefinition;
    }
}
