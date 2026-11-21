using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.SyntaxWalkers;
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

        public void ApplyImplementDirective()
        {
            var implementDirectiveExists =
                field.Directives.AsEnumerable().Any(d => d.Name == DirectiveNames.Implement);

            if (!implementDirectiveExists)
            {
                field.Directives.Add(new Directive(FusionBuiltIns.SourceSchemaDirectives[DirectiveNames.Implement]));
            }
        }

        public bool ExistsInSchema(string schemaName)
        {
            return field.Directives.AsEnumerable().Any(
                d =>
                    d.Name == DirectiveNames.FusionField
                    && (string)d.Arguments[ArgumentNames.Schema].Value! == schemaName);
        }

        /// <summary>
        /// Determines whether the field is a <c>viewer</c> field without arguments that returns the
        /// type of <paramref name="queryViewerField"/>.
        /// </summary>
        public bool IsViewerFieldFor(MutableOutputFieldDefinition queryViewerField)
            => field is { Name: WellKnownFieldNames.Viewer, Arguments.Count: 0 }
                && ReferenceEquals(field.Type.NamedType(), queryViewerField.Type.NamedType());

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

        public ImmutableArray<string> GetFusionEventStreamSchemaNames()
        {
            ImmutableArray<string>.Builder? builder = null;

            foreach (var directive in field.Directives.AsEnumerable())
            {
                if (directive.Name != DirectiveNames.FusionEventStream)
                {
                    continue;
                }

                builder ??= ImmutableArray.CreateBuilder<string>();
                builder.Add((string)directive.Arguments[ArgumentNames.Schema].Value!);
            }

            return builder?.ToImmutable() ?? [];
        }

        public SelectionSetNode? GetFusionEventStreamMessage(string schemaName)
        {
            var fusionEventStreamDirective =
                field.Directives.AsEnumerable().FirstOrDefault(
                    d =>
                        d.Name == DirectiveNames.FusionEventStream
                        && (string)d.Arguments[ArgumentNames.Schema].Value! == schemaName);

            if (fusionEventStreamDirective?.Arguments.TryGetValue(ArgumentNames.Message, out var message) == true)
            {
                var selectionSet = ParseSelectionSet((string)message.Value!);

                if (fusionEventStreamDirective.Arguments.TryGetValue(ArgumentNames.CursorField, out var cursorField)
                    && cursorField.Value is string cursorFieldName)
                {
                    return selectionSet.WithSelections(
                        [.. selectionSet.Selections, new FieldNode(cursorFieldName)]);
                }

                return selectionSet;
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

            if (!fusionRequiresDirective.Arguments.TryGetValue(
                    ArgumentNames.Requirements,
                    out var requirements))
            {
                return null;
            }

            return ParseSelectionSet($"{{ {(string)requirements.Value!} }}");
        }

        /// <summary>
        /// Gets the distinct names of the contexts that the <c>map</c> of the <c>@fusion__requires</c>
        /// directive of <paramref name="schemaName"/> reads, or an empty array if it reads none.
        /// </summary>
        public ImmutableArray<string> GetFusionRequiresContextNames(string schemaName)
        {
            var fusionRequiresDirective =
                field.Directives
                    .AsEnumerable()
                    .FirstOrDefault(
                        d =>
                            d.Name == DirectiveNames.FusionRequires
                            && (string)d.Arguments[ArgumentNames.Schema].Value! == schemaName);

            if (fusionRequiresDirective is null
                || !fusionRequiresDirective.Arguments.TryGetValue(ArgumentNames.Map, out var map)
                || map is not HotChocolate.Language.ListValueNode mapList)
            {
                return [];
            }

            ImmutableArray<string>.Builder? builder = null;

            foreach (var item in mapList.Items)
            {
                if (item.Value is not string mapEntry)
                {
                    continue;
                }

                var selectedValue = new FieldSelectionMapParser(mapEntry).Parse();

                foreach (var contextName in FieldSelectionMapContextNamesExtractor.Instance.Extract(selectedValue))
                {
                    builder ??= ImmutableArray.CreateBuilder<string>();

                    if (!builder.Contains(contextName))
                    {
                        builder.Add(contextName);
                    }
                }
            }

            return builder?.ToImmutable() ?? [];
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
