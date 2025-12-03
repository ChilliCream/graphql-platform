using System.Collections.Immutable;
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
    extension(MutableOutputFieldDefinition field)
    {
        public void ApplyLookupDirective()
        {
            var lookupDirectiveExists =
                field.Directives.AsEnumerable().Any(d => d.Name == DirectiveNames.Lookup);

            if (!lookupDirectiveExists)
            {
                field.Directives.Add(new Directive(FusionBuiltIns.SourceSchemaDirectives[DirectiveNames.Lookup]));
            }
        }

        public void ApplyShareableDirective()
        {
            var shareableDirectiveExists =
                field.Directives.AsEnumerable().Any(d => d.Name == DirectiveNames.Shareable);

            if (!shareableDirectiveExists)
            {
                field.Directives.Add(new Directive(FusionBuiltIns.SourceSchemaDirectives[DirectiveNames.Shareable]));
            }
        }

        public bool ExistsInSchema(string schemaName)
        {
            return field.Directives.AsEnumerable().Any(
                d =>
                    d.Name == DirectiveNames.FusionField
                    && (string)d.Arguments[ArgumentNames.Schema].Value! == schemaName);
        }

        public string? GetFusionFieldProvides(string schemaName)
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

        public List<string> GetFusionLookupMap()
        {
            var items = new List<string>();

            foreach (var argument in field.Arguments)
            {
                var @is = argument.GetIsFieldSelectionMap();

                items.Add(@is ?? argument.Name);
            }

            return items;
        }

        public SelectionSetNode? GetFusionRequiresRequirements(string schemaName)
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

        /// <summary>
        /// Rewrites the provided value selections into a selection set string that can be used in a key
        /// directive.
        /// <example>["a", "b.c"] -> "a b { c }"</example>
        /// </summary>
        public string GetKeyFields(List<IValueSelectionNode> valueSelections,
            MutableSchemaDefinition schema)
        {
            var valueSelectionToSelectionSetRewriter =
                new ValueSelectionToSelectionSetRewriter(schema, ignoreMissingTypeSystemMembers: true);
            var fieldType = field.Type.AsTypeDefinition();
            var selectionSets = valueSelections
                .Select(s => valueSelectionToSelectionSetRewriter.Rewrite(s, fieldType))
                .ToImmutableArray();
            var mergedSelectionSet = selectionSets.Length == 1
                ? selectionSets[0]
                : new MergeSelectionSetRewriter(schema).Merge(selectionSets, fieldType);

            return mergedSelectionSet.ToString(indented: false).AsSpan()[2..^2].ToString();
        }

        public string? GetOverrideFrom()
        {
            var overrideDirective =
                field.Directives.AsEnumerable().SingleOrDefault(d => d.Name == DirectiveNames.Override);

            return (string?)overrideDirective?.Arguments[ArgumentNames.From].Value;
        }

        /// <summary>
        /// Gets all combinations of value selections after splitting choice nodes.
        /// <example>["a", "{ b } | { c }"] -> [["a", "b"], ["a", "c"]]</example>
        /// </summary>
        public List<List<IValueSelectionNode>> GetValueSelectionGroups()
        {
            var lookupMap = field.GetFusionLookupMap();
            var valueSelections = lookupMap.Select(a => new FieldSelectionMapParser(a).Parse());

            var valueSelectionGroups = valueSelections.Select(v =>
            {
                List<IValueSelectionNode> items = [];

                if (v is ChoiceValueSelectionNode choiceValueSelectionNode)
                {
                    items.AddRange(choiceValueSelectionNode.Branches.ToArray());
                }
                else
                {
                    items.Add(v);
                }

                return items.ToArray();
            });

            return GetAllCombinations(valueSelectionGroups.ToArray());
        }

        public bool IsPartial(string schemaName)
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

    // productById(id: ID!) -> ["id"].
    // productByIdAndCategoryId(id: ID!, categoryId: Int) -> ["id", "categoryId"].
    // personByAddressId(id: ID! @is(field: "address.id")) -> ["address.id"].

    private static List<List<T>> GetAllCombinations<T>(params T[][] arrays)
    {
        if (arrays.Length == 0)
        {
            return [[]];
        }

        var result = new List<List<T>> { new() };

        foreach (var array in arrays)
        {
            var temp = new List<List<T>>();

            foreach (var item in array)
            {
                foreach (var combination in result)
                {
                    var newCombination = new List<T>(combination) { item };
                    temp.Add(newCombination);
                }
            }

            result = temp;
        }

        return result;
    }
}
