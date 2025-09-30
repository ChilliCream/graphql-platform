using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Features;
using HotChocolate.Types;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class OutputFieldDefinitionExtensions
{
    public static ImmutableArray<string> GetSchemaNames(
        this IOutputFieldDefinition field,
        string? first = null)
    {
        var fusionFieldDirectives =
            field.Directives.AsEnumerable().Where(d => d.Name == FusionField);

        var schemaNames =
            fusionFieldDirectives.Select(d => (string)d.Arguments[Schema].Value!).ToList();

        if (first is not null && schemaNames.Contains(first))
        {
            schemaNames = schemaNames.Where(n => n != first).Prepend(first).ToList();
        }

        return [.. schemaNames];
    }

    public static SourceFieldMetadata GetRequiredSourceFieldMetadata(
        this IOutputFieldDefinition field)
    {
        return field.Features.GetRequired<SourceFieldMetadata>();
    }
}
