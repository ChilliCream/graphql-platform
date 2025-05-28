using System.Collections.Immutable;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableOutputFieldDefinitionExtensions
{
    public static void ApplyLookupDirective(this MutableOutputFieldDefinition field)
        => field.Directives.Add(new Directive(new MutableDirectiveDefinition(Lookup)));

    public static ImmutableArray<string> GetSchemaNames(this IOutputFieldDefinition field)
    {
        var fusionFieldDirectives = field.Directives.AsEnumerable().Where(d => d.Name == FusionField);
        return [.. fusionFieldDirectives.Select(d => (string)d.Arguments[Schema].Value!)];
    }

    public static bool HasInternalDirective(this MutableOutputFieldDefinition type)
        => type.Directives.ContainsName(Internal);
}
