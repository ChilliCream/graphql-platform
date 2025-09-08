using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableObjectTypeDefinitionExtensions
{
    public static void ApplyShareableDirective(this MutableObjectTypeDefinition type)
    {
        type.Directives.Add(new Directive(new MutableDirectiveDefinition(DirectiveNames.Shareable)));
    }

    public static bool HasInternalDirective(this MutableObjectTypeDefinition type)
    {
        return type.Directives.ContainsName(DirectiveNames.Internal);
    }
}
