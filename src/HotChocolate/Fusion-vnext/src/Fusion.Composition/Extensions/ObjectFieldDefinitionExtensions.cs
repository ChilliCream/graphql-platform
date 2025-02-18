using HotChocolate.Skimmed;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

public static class ObjectFieldDefinitionExtensions
{
    public static void ApplyLookupDirective(this OutputFieldDefinition outputField)
    {
        outputField.Directives.Add(new Directive(new DirectiveDefinition(DirectiveNames.Lookup)));
    }
}
