using System.Collections.Immutable;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableOutputFieldDefinitionExtensions
{
    public static void ApplyLookupDirective(this MutableOutputFieldDefinition field,
        LookupMutableDirectiveDefinition lookupDirectiveDefinition)
    {
        field.Directives.Add(new Directive(lookupDirectiveDefinition));
    }

    public static ImmutableArray<string> GetSchemaNames(this MutableOutputFieldDefinition field)
    {
        var fusionFieldDirectives =
            field.Directives.AsEnumerable().Where(d => d.Name == FusionField);

        return [.. fusionFieldDirectives.Select(d => (string)d.Arguments[Schema].Value!)];
    }

    public static bool HasInternalDirective(this MutableOutputFieldDefinition type)
    {
        return type.Directives.ContainsName(Internal);
    }
}
