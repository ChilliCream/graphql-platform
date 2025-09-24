using System.Collections.Immutable;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableOutputFieldDefinitionExtensions
{
    public static void ApplyShareableDirective(this MutableOutputFieldDefinition field)
    {
        if (field.Directives.ContainsName(DirectiveNames.Shareable))
        {
            return;
        }

        field.Directives.Add(new Directive(new ShareableMutableDirectiveDefinition()));
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

    // productById(id: ID!) -> ["id"].
    // productByIdAndCategoryId(id: ID!, categoryId: Int) -> ["id", "categoryId"].
    // personByAddressId(id: ID! @is(field: "address.id")) -> ["address.id"].
    public static List<string> GetFusionLookupMap(this MutableOutputFieldDefinition field)
    {
        var items = new List<string>();

        foreach (var argument in field.Arguments)
        {
            var @is = argument.GetIsFieldSelectionMap();

            items.Add(@is ?? argument.Name);
        }

        return items;
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

    public static string GetKeyFields(
        this MutableOutputFieldDefinition field,
        List<string> lookupMap,
        MutableSchemaDefinition schema)
    {
        var selectedValues = lookupMap.Select(a => new FieldSelectionMapParser(a).Parse());
        var valueSelectionToSelectionSetRewriter = new ValueSelectionToSelectionSetRewriter(schema);
        var fieldType = field.Type.AsTypeDefinition();
        var selectionSets = selectedValues
            .Select(s => valueSelectionToSelectionSetRewriter.Rewrite(s, fieldType))
            .ToImmutableArray();
        var mergedSelectionSet = selectionSets.Length == 1
            ? selectionSets[0]
            : new MergeSelectionSetRewriter(schema).Merge(selectionSets, fieldType);

        return mergedSelectionSet.ToString(indented: false).AsSpan()[2..^2].ToString();
    }

    public static string? GetOverrideFrom(this MutableOutputFieldDefinition field)
    {
        var overrideDirective =
            field.Directives.AsEnumerable().SingleOrDefault(d => d.Name == DirectiveNames.Override);

        return (string?)overrideDirective?.Arguments[ArgumentNames.From].Value;
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
