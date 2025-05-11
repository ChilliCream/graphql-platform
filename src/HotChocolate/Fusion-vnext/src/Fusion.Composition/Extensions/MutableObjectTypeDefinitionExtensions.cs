using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableObjectTypeDefinitionExtensions
{
    public static void ApplyShareableDirective(this MutableObjectTypeDefinition type,
        ShareableMutableDirectiveDefinition shareableDirectiveDefinition)
    {
        type.Directives.Add(new Directive(shareableDirectiveDefinition));
    }

    public static bool HasInternalDirective(this MutableObjectTypeDefinition type)
    {
        return type.Directives.ContainsName(Internal);
    }
}
