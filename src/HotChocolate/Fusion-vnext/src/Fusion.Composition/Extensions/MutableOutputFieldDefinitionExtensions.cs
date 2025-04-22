using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableOutputFieldDefinitionExtensions
{
    public static void ApplyLookupDirective(this MutableOutputFieldDefinition field)
    {
        field.Directives.Add(new Directive(new MutableDirectiveDefinition(Lookup)));
    }

    public static ImmutableArray<string> GetFusionRequiresMap(
        this MutableOutputFieldDefinition field,
        string schemaName)
    {
        var fusionRequiresDirectives =
            field.Directives
                .AsEnumerable()
                .Where(
                    d =>
                        d.Name == FusionRequires
                        && (string)d.Arguments[Schema].Value! == schemaName);

        var map = fusionRequiresDirectives.SelectMany(
            d => ((ListValueNode)d.Arguments[Map]).Items.Select(i => ((StringValueNode)i).Value));

        return [.. map];
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
