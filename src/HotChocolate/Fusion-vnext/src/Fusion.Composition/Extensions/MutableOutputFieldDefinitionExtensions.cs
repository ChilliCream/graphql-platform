using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableOutputFieldDefinitionExtensions
{
    public static void ApplyLookupDirective(this MutableOutputFieldDefinition field)
    {
        field.Directives.Add(new Directive(new MutableDirectiveDefinition(Lookup)));
    }

    public static bool ExistsInSchema(this MutableOutputFieldDefinition field, string schemaName)
    {
        return field.Directives.AsEnumerable().Any(
            d => d.Name == FusionField && (string)d.Arguments[Schema].Value! == schemaName);
    }

    public static string? GetFusionFieldProvides(
        this MutableOutputFieldDefinition field,
        string schemaName)
    {
        var fusionFieldDirective =
            field.Directives.AsEnumerable().FirstOrDefault(
                d =>
                    d.Name == FusionField
                    && (string)d.Arguments[Schema].Value! == schemaName);

        if (fusionFieldDirective?.Arguments.TryGetValue(
                WellKnownArgumentNames.Provides,
                out var provides) == true)
        {
            return (string?)provides.Value;
        }

        return null;
    }

    public static SelectionSetNode? GetFusionRequiresRequirements(
        this MutableOutputFieldDefinition field,
        string schemaName)
    {
        var fusionRequiresDirective =
            field.Directives
                .AsEnumerable()
                .FirstOrDefault(
                    d =>
                        d.Name == FusionRequires
                        && (string)d.Arguments[Schema].Value! == schemaName);

        if (fusionRequiresDirective is null)
        {
            return null;
        }

        var requirements = (string)fusionRequiresDirective.Arguments[Requirements].Value!;

        return ParseSelectionSet($"{{ {requirements} }}");
    }

    public static bool HasInternalDirective(this MutableOutputFieldDefinition type)
    {
        return type.Directives.ContainsName(Internal);
    }

    public static bool IsPartial(this MutableOutputFieldDefinition field, string schemaName)
    {
        var fusionFieldDirective =
            field.Directives.AsEnumerable().FirstOrDefault(
                d =>
                    d.Name == FusionField
                    && (string)d.Arguments[Schema].Value! == schemaName);

        if (fusionFieldDirective is null)
        {
            return false;
        }

        if (fusionFieldDirective.Arguments.TryGetValue(Partial, out var partial))
        {
            return (bool)partial.Value!;
        }

        return false;
    }
}
