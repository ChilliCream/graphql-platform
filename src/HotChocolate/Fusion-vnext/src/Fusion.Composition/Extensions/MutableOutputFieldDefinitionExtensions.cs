using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableOutputFieldDefinitionExtensions
{
    public static void ApplyLookupDirective(this MutableOutputFieldDefinition field)
    {
        field.Directives.Add(new Directive(new MutableDirectiveDefinition(DirectiveNames.Lookup)));
    }

    public static bool ExistsInSchema(this MutableOutputFieldDefinition field, string schemaName)
    {
        return field.Directives.AsEnumerable().Any(
            d =>
                d.Name == DirectiveNames.FusionField
                && (string)d.Arguments[ArgumentNames.Schema].Value! == schemaName);
    }

    public static string? GetFusionFieldProvides(
        this MutableOutputFieldDefinition field,
        string schemaName)
    {
        var fusionFieldDirective =
            field.Directives.AsEnumerable().FirstOrDefault(
                d =>
                    d.Name == DirectiveNames.FusionField
                    && (string)d.Arguments[ArgumentNames.Schema].Value! == schemaName);

        if (fusionFieldDirective?.Arguments.TryGetValue(ArgumentNames.Provides, out var provides) == true)
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
                        d.Name == DirectiveNames.FusionRequires
                        && (string)d.Arguments[ArgumentNames.Schema].Value! == schemaName);

        if (fusionRequiresDirective is null)
        {
            return null;
        }

        var requirements = (string)fusionRequiresDirective.Arguments[ArgumentNames.Requirements].Value!;

        return ParseSelectionSet($"{{ {requirements} }}");
    }

    public static bool HasInternalDirective(this MutableOutputFieldDefinition type)
    {
        return type.Directives.ContainsName(DirectiveNames.Internal);
    }

    public static bool IsPartial(this MutableOutputFieldDefinition field, string schemaName)
    {
        var fusionFieldDirective =
            field.Directives.AsEnumerable().FirstOrDefault(
                d =>
                    d.Name == DirectiveNames.FusionField
                    && (string)d.Arguments[ArgumentNames.Schema].Value! == schemaName);

        if (fusionFieldDirective is null)
        {
            return false;
        }

        if (fusionFieldDirective.Arguments.TryGetValue(ArgumentNames.Partial, out var partial))
        {
            return (bool)partial.Value!;
        }

        return false;
    }
}
