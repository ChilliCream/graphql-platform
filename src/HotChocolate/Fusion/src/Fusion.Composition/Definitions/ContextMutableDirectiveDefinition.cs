using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@context</c> directive declares that the values of a type set a named context, which
/// fields of other types can reference in a <c>@require</c> or <c>@is</c> field selection map.
/// </summary>
internal sealed class ContextMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public ContextMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(WellKnownDirectiveNames.Context)
    {
        Description = ContextMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Name,
                new NonNullType(stringType))
            {
                Description = ContextMutableDirectiveDefinition_Argument_Name_Description
            });

        IsRepeatable = true;

        Locations = DirectiveLocation.Object | DirectiveLocation.Interface | DirectiveLocation.Union;
    }
}
