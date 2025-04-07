using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// Implements the field selection merging rule as described in the GraphQL Spec.
/// https://spec.graphql.org/June2018/#sec-Field-Selection-Merging
/// </summary>
/// <remarks>
/// The algorithm implemented here is not the one from the Spec, but is based on
/// https://tech.xing.com/graphql-overlapping-fields-can-be-merged-fast-ea6e92e0a01
/// It is not the final version (Listing 11), but Listing 10 adopted to this code base.
/// </remarks>
internal sealed class OverlappingFieldsCanBeMergedRule : IDocumentValidatorRule
{
    public ushort Priority => ushort.MaxValue;

    public bool IsCacheable => true;

    public void Validate(IDocumentValidatorContext context, DocumentNode document)
    {
        ValidateInternal(new MergeContext(context), document);
    }

    private static void ValidateInternal(MergeContext context, DocumentNode document)
    {
        foreach (var operation in document.Definitions)
        {
            if (operation is not OperationDefinitionNode operationDef)
            {
                continue;
            }

            if (context.Schema.GetOperationType(operationDef.Operation) is not { } rootType)
            {
                continue;
            }

            var fieldMap = new Dictionary<string, HashSet<FieldAndType>>();
            var visitedFragmentSpreads = new HashSet<string>();
            CollectFields(context, fieldMap, operationDef.SelectionSet, rootType, visitedFragmentSpreads);
            var conflicts = FindConflicts(context, fieldMap);

            foreach (var conflict in conflicts)
            {
                if (context.ConflictsReported.Any(r => r.SetEquals(conflict.Fields)))
                {
                    continue;
                }

                context.ConflictsReported.Add(conflict.Fields);

                context.ReportError(
                    ErrorBuilder.New()
                        .SetMessage(conflict.Reason)
                        .SetLocations(conflict.Fields.ToList())
                        .Build());
            }
        }
    }

    private static void CollectFields(
        MergeContext context,
        Dictionary<string, HashSet<FieldAndType>> fieldMap,
        SelectionSetNode selectionSet, IType parentType,
        HashSet<string> visitedFragmentSpreads)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    CollectFieldsForField(fieldMap, parentType, field);
                    break;

                case InlineFragmentNode inlineFragment:
                    var type = inlineFragment.TypeCondition is null
                        ? parentType
                        : context.Schema.GetType<INamedType>(inlineFragment.TypeCondition.Name.Value);
                    CollectFields(context, fieldMap, inlineFragment.SelectionSet, type, visitedFragmentSpreads);
                    break;

                case FragmentSpreadNode spread:
                    if (!visitedFragmentSpreads.Add(spread.Name.Value))
                    {
                        continue;
                    }

                    if (context.Fragments.TryGetValue(spread.Name.Value, out var fragment))
                    {
                        var fragType = context.Schema.GetType<INamedType>(fragment.TypeCondition.Name.Value);
                        CollectFields(context, fieldMap, fragment.SelectionSet, fragType, visitedFragmentSpreads);
                    }

                    break;
            }
        }
    }

    private static void CollectFieldsForField(
        Dictionary<string, HashSet<FieldAndType>> fieldMap,
        IType parentType, FieldNode field)
    {
        var responseName = field.Alias?.Value ?? field.Name.Value;
        if (!fieldMap.TryGetValue(responseName, out var fields))
        {
            fields = [];
            fieldMap[responseName] = fields;
        }

        if (parentType is IComplexOutputType namedType
            && namedType.Fields.TryGetField(field.Name.Value, out var fieldDef))
        {
            fields.Add(new FieldAndType(field, fieldDef.Type, namedType));
        }
        else
        {
            fields.Add(new FieldAndType(field, null, parentType));
        }
    }

    private static List<Conflict> FindConflicts(
        MergeContext context,
        Dictionary<string, HashSet<FieldAndType>> fieldMap)
    {
        var result = new List<Conflict>();

        SameResponseShapeByName(context, fieldMap, [], result);
        SameForCommonParentsByName(context, fieldMap, [], result);

        return result;
    }

    private static void SameResponseShapeByName(
        MergeContext context,
        Dictionary<string, HashSet<FieldAndType>> fieldMap,
        List<string> path,
        List<Conflict> conflictsResult)
    {
        foreach (var entry in fieldMap)
        {
            if (context.SameResponseShapeChecked.Any(set => set.SetEquals(entry.Value)))
            {
                continue;
            }

            context.SameResponseShapeChecked.Add(entry.Value);

            var newPath = new List<string>(path) { entry.Key };
            var conflict = RequireSameOutputTypeShape(entry.Value);

            if (conflict != null)
            {
                conflictsResult.Add(conflict);
                continue;
            }

            var subSelections = MergeSubSelections(context, entry.Value);
            SameResponseShapeByName(context, subSelections, newPath, conflictsResult);
        }
    }

    private static void SameForCommonParentsByName(
        MergeContext context,
        Dictionary<string, HashSet<FieldAndType>> fieldMap,
        List<string> path,
        List<Conflict> conflictsResult)
    {
        foreach (var entry in fieldMap)
        {
            var groups = GroupByCommonParents(entry.Value);
            var newPath = new List<string>(path) { entry.Key };

            foreach (var group in groups)
            {
                if (context.SameForCommonParentsChecked.Any(g => g.SetEquals(group)))
                {
                    continue;
                }

                context.SameForCommonParentsChecked.Add(group);

                var conflict = RequireSameNameAndArguments(newPath, group);
                if (conflict is not null)
                {
                    conflictsResult.Add(conflict);
                    continue;
                }

                var subSelections = MergeSubSelections(context, group);
                SameForCommonParentsByName(context, subSelections, newPath, conflictsResult);
            }
        }
    }

    private static List<HashSet<FieldAndType>> GroupByCommonParents(HashSet<FieldAndType> fields)
    {
        var concreteGroups = fields
            .Where(f => f.ParentType is ObjectType)
            .GroupBy(f => f.ParentType)
            .ToList();

        if (concreteGroups.Count == 0)
        {
            return [fields];
        }

        var abstractTypes = fields
            .Where(f => f.ParentType.Kind is TypeKind.Interface or TypeKind.Union)
            .ToList();

        var groupByCommonParents = new List<HashSet<FieldAndType>>();

        foreach (var group in concreteGroups)
        {
            var set = new HashSet<FieldAndType>(group);

            foreach (var abstractType in abstractTypes)
            {
                set.Add(abstractType);
            }

            groupByCommonParents.Add(set);
        }

        return groupByCommonParents;
    }

    private static Conflict? RequireSameNameAndArguments(List<string> path, HashSet<FieldAndType> fieldGroup)
    {
        if (fieldGroup.Count <= 1)
        {
            return null;
        }

        var first = fieldGroup.First();
        var name = first.Field.Name.Value;
        var arguments = first.Field.Arguments;

        foreach (var (field, _, _) in fieldGroup.Skip(1))
        {
            if (field.Name.Value != name || !SameArguments(arguments, field.Arguments))
            {
                return new Conflict(
                    $"Fields conflict at {string.Join("/", path)}: different names or arguments.",
                    fieldGroup.Select(f => f.Field).ToHashSet());
            }
        }

        return null;
    }

    private static bool SameArguments(IReadOnlyList<ArgumentNode> a, IReadOnlyList<ArgumentNode> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        if (a.Count == 0)
        {
            return true;
        }

        for (var i = 0; i < a.Count; i++)
        {
            var argA = a[i];
            var argB = b.FirstOrDefault(x => x.Name.Value == argA.Name.Value);
            if (argB is null || !argA.Value.Equals(argB.Value, SyntaxComparison.Syntax))
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, HashSet<FieldAndType>> MergeSubSelections(
        MergeContext context,
        HashSet<FieldAndType> fields)
    {
        var merged = new Dictionary<string, HashSet<FieldAndType>>();

        foreach (var item in fields)
        {
            if (item.Field.SelectionSet is null)
            {
                continue;
            }

            var visited = new HashSet<string>();
            CollectFields(context, merged, item.Field.SelectionSet, item.Type!, visited);
        }

        return merged;
    }

    private static Conflict? RequireSameOutputTypeShape(HashSet<FieldAndType> fields)
    {
        if (fields.Count <= 1)
        {
            return null;
        }

        var list = fields.ToList();
        var baseField = list[0];

        foreach (var current in list.Skip(1))
        {
            var a = baseField.Type;
            var b = current.Type;

            while (true)
            {
                if (a is NonNullType || b is NonNullType)
                {
                    if (a is not NonNullType || b is not NonNullType)
                    {
                        return new Conflict(
                            "Fields have different nullability",
                            fields.Select(f => f.Field).ToHashSet());
                    }
                }

                if (a is ListType || b is ListType)
                {
                    if (a is not ListType || b is not ListType)
                    {
                        return new Conflict(
                            "Fields have different list structure",
                            fields.Select(f => f.Field).ToHashSet());
                    }
                }

                if (a is not (NonNullType or ListType) && b is not (NonNullType or ListType))
                {
                    break;
                }

                a = a?.InnerType();
                b = b?.InnerType();
            }

            if (!SameType(a, b))
            {
                return new Conflict(
                    "Fields have different base types",
                    fields.Select(f => f.Field).ToHashSet());
            }
        }

        return null;
    }

    private static bool SameType(IType? a, IType? b)
    {
        if (a is null)
        {
            return b is null;
        }

        return a.Equals(b);
    }

    private sealed class FieldAndType(FieldNode field, IType? type, IType parentType)
    {
        public FieldNode Field { get; } = field;

        public IType? Type { get; } = type;

        public IType ParentType { get; } = parentType;

        public override bool Equals(object? obj)
            => obj is FieldAndType other && Field.Equals(other.Field);

        public override int GetHashCode()
            => Field.GetHashCode();

        public void Deconstruct(out FieldNode field, out IType? type, out IType parentType)
        {
            field = Field;
            type = Type;
            parentType = ParentType;
        }
    }

    private sealed class Conflict(string reason, HashSet<FieldNode> fields)
    {
        public string Reason { get; } = reason;

        public HashSet<FieldNode> Fields { get; } = fields;
    }

    private class HashSetComparer<T> : IEqualityComparer<HashSet<T>> where T : notnull
    {
        public static readonly HashSetComparer<T> Instance = new();

        public bool Equals(HashSet<T>? x, HashSet<T>? y)
            => x != null && y != null && x.SetEquals(y);

        public int GetHashCode(HashSet<T> obj)
        {
            var hash = new HashCode();

            foreach (var item in obj)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }
    }

    private sealed class MergeContext(IDocumentValidatorContext context)
    {
        public ISchema Schema => context.Schema;

        public HashSet<HashSet<FieldAndType>> SameResponseShapeChecked { get; }
            = new(HashSetComparer<FieldAndType>.Instance);

        public HashSet<HashSet<FieldAndType>> SameForCommonParentsChecked { get; }
            = new(HashSetComparer<FieldAndType>.Instance);

        public HashSet<HashSet<FieldNode>> ConflictsReported { get; }
            = new(HashSetComparer<FieldNode>.Instance);

        public IDictionary<string, FragmentDefinitionNode> Fragments => context.Fragments;

        public void ReportError(IError error) => context.ReportError(error);
    }
}
