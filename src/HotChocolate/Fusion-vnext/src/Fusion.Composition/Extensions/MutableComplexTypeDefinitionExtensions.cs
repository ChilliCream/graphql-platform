using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableComplexTypeDefinitionExtensions
{
    public static void ApplyKeyDirective(this MutableComplexTypeDefinition type, string[] fields)
    {
        var fieldsArgument = new ArgumentAssignment(ArgumentNames.Fields, string.Join(" ", fields));
        var keyDirective =
            new Directive(new MutableDirectiveDefinition(DirectiveNames.Key), fieldsArgument);

        type.Directives.Add(keyDirective);
    }
}
