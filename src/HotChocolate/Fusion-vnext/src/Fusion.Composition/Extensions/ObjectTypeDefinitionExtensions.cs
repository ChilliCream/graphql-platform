using HotChocolate.Skimmed;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class ObjectTypeDefinitionExtensions
{
    public static void ApplyShareableDirective(this ObjectTypeDefinition type)
    {
        type.Directives.Add(new Directive(new DirectiveDefinition(DirectiveNames.Shareable)));
    }
}
