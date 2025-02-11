using HotChocolate.Types.Mutable;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

public static class ObjectFieldDefinitionExtensions
{
    public static void ApplyLookupDirective(this MutableOutputFieldDefinition outputField)
    {
        outputField.Directives.Add(new Directive(new MutableDirectiveDefinition(DirectiveNames.Lookup)));
    }
}
