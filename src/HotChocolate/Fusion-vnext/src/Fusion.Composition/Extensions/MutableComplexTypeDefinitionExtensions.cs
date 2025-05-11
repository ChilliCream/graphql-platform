using HotChocolate.Fusion.Definitions;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableComplexTypeDefinitionExtensions
{
    public static void ApplyKeyDirective(this MutableComplexTypeDefinition type,
        KeyMutableDirectiveDefinition keyDirectiveDefinition, string[] fields)
    {
        var fieldsArgument = new ArgumentAssignment(Fields, string.Join(" ", fields));
        var keyDirective = new Directive(keyDirectiveDefinition, fieldsArgument);

        type.Directives.Add(keyDirective);
    }
}
