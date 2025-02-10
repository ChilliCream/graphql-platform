using HotChocolate.Skimmed;
using HotChocolate.Types;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class ComplexTypeDefinitionExtensions
{
    public static void ApplyKeyDirective(this ComplexTypeDefinition type, string[] fields)
    {
        var fieldsArgument = new ArgumentAssignment(ArgumentNames.Fields, string.Join(" ", fields));
        var keyDirective = new Directive(new DirectiveDefinition(DirectiveNames.Key), fieldsArgument);

        type.Directives.Add(keyDirective);
    }
}
