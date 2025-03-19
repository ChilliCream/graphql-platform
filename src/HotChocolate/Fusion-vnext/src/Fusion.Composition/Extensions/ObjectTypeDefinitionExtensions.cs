using HotChocolate.Types.Mutable;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class ObjectTypeDefinitionExtensions
{
    public static void ApplyShareableDirective(this MutableObjectTypeDefinition type)
    {
        type.Directives.Add(new Directive(new MutableDirectiveDefinition(DirectiveNames.Shareable)));
    }
}
