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
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

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
                        .SetPath(conflict.Path)
                        .SpecifiedBy("sec-Field-Selection-Merging")
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
        IType parentType,
        FieldNode field)
    {
        var responseName = field.Alias?.Value ?? field.Name.Value;
        if (!fieldMap.TryGetValue(responseName, out var fields))
        {
            fields = [];
            fieldMap[responseName] = fields;
        }

        var unwrappedParentType = parentType.NamedType();

        if (unwrappedParentType is IComplexOutputType complexType
            && complexType.Fields.TryGetField(field.Name.Value, out var fieldDef))
        {
            fields.Add(new FieldAndType(field, fieldDef.Type, complexType));
        }
        else
        {
            fields.Add(new FieldAndType(field, null, unwrappedParentType));
        }
    }

    private static List<Conflict> FindConflicts(
        MergeContext context,
        Dictionary<string, HashSet<FieldAndType>> fieldMap)
    {
        var result = new List<Conflict>();

        SameResponseShapeByName(context, fieldMap, Path.Root, result);
        SameForCommonParentsByName(context, fieldMap, Path.Root, result);

        return result;
    }

    private static void SameResponseShapeByName(
        MergeContext context,
        Dictionary<string, HashSet<FieldAndType>> fieldMap,
        Path path,
        List<Conflict> conflictsResult)
    {
        foreach (var entry in fieldMap)
        {
            if (context.SameResponseShapeChecked.Any(set => set.SetEquals(entry.Value)))
            {
                continue;
            }

            context.SameResponseShapeChecked.Add(entry.Value);

            var newPath = path.Append(entry.Key);
            var conflict = RequireSameOutputTypeShape(entry.Value, newPath);

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
        Path path,
        List<Conflict> conflictsResult)
    {
        foreach (var entry in fieldMap)
        {
            var groups = GroupByCommonParents(entry.Value);
            var newPath = path.Append(entry.Key);

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
        // Separate abstract and concrete parent types
        var abstractFields = new List<FieldAndType>();
        var concreteGroups = new Dictionary<IType, List<FieldAndType>>();

        foreach (var field in fields)
        {
            var parent = field.ParentType.NamedType();

            switch (parent.Kind)
            {
                case TypeKind.Interface or TypeKind.Union:
                    abstractFields.Add(field);
                    break;

                case TypeKind.Object:
                {
                    if (!concreteGroups.TryGetValue(parent, out var list))
                    {
                        list = [];
                        concreteGroups[parent] = list;
                    }

                    list.Add(field);
                    break;
                }
            }
        }

        // If there are no concrete groups, just return one group with all abstract fields
        if (concreteGroups.Count == 0)
        {
            return [new HashSet<FieldAndType>(abstractFields)];
        }

        var result = new List<HashSet<FieldAndType>>();

        foreach (var group in concreteGroups.Values)
        {
            var set = new HashSet<FieldAndType>(group);

            foreach (var abstractField in abstractFields)
            {
                set.Add(abstractField);
            }

            result.Add(set);
        }

        return result;
    }

    private static Conflict? RequireSameNameAndArguments(Path path, HashSet<FieldAndType> fieldGroup)
    {
        if (fieldGroup.Count <= 1)
        {
            return null;
        }

        var first = fieldGroup.First();
        var name = first.Field.Name.Value;
        var arguments = first.Field.Arguments;
        var responseName = first.Field.Alias?.Value ?? name;

        foreach (var (field, _, _) in fieldGroup.Skip(1))
        {
            if (field.Name.Value != name)
            {
                return NameMismatchConflict(name, field.Name.Value, path, fieldGroup);
            }

            if (!SameArguments(arguments, field.Arguments))
            {
                return ArgumentMismatchConflict(responseName, path, fieldGroup);
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
        HashSet<string>? visited = null;

        foreach (var item in fields)
        {
            if (item.Field.SelectionSet is null || item.Type is null)
            {
                continue;
            }

            visited ??= [];
            visited.Clear();

            CollectFields(context, merged, item.Field.SelectionSet, item.Type, visited);
        }

        return merged;
    }

    private static Conflict? RequireSameOutputTypeShape(
        HashSet<FieldAndType> fields,
        Path path)
    {
        if (fields.Count <= 1)
        {
            return null;
        }

        var baseField = fields.First();

        foreach (var current in fields.Skip(1))
        {
            var a = baseField.Type;
            var b = current.Type;

            while (true)
            {
                if (a is NonNullType || b is NonNullType)
                {
                    if (a is not NonNullType || b is not NonNullType)
                    {
                        return TypeMismatchConflict(
                            current.Field.Name.Value,
                            path,
                            a,
                            b,
                            fields);
                    }
                }

                if (a is ListType || b is ListType)
                {
                    if (a is not ListType || b is not ListType)
                    {
                        return TypeMismatchConflict(
                            current.Field.Name.Value,
                            path,
                            a,
                            b,
                            fields);
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
                return TypeMismatchConflict(
                    current.Field.Name.Value,
                    path,
                    a,
                    b,
                    fields);
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

    private static Conflict TypeMismatchConflict(
        string responseName,
        Path path,
        IType? typeA,
        IType? typeB,
        HashSet<FieldAndType> fields)
    {
        var typeNameA = typeA?.Print() ?? "null";
        var typeNameB = typeB?.Print() ?? "null";

        var fieldNodes = new HashSet<FieldNode>(SyntaxComparer.BySyntax);

        foreach (var field in fields)
        {
            fieldNodes.Add(field.Field);
        }

        return new Conflict(
            string.Format(
                "Fields `{0}` conflict because they return conflicting types `{1}` and `{2}`. "
                + "Use different aliases on the fields to fetch both if this was intentional.",
                responseName,
                typeNameA,
                typeNameB),
            fieldNodes,
            path);
    }

    private static Conflict ArgumentMismatchConflict(
        string responseName,
        Path path,
        HashSet<FieldAndType> fields)
    {
        var fieldNodes = new HashSet<FieldNode>(SyntaxComparer.BySyntax);

        foreach (var field in fields)
        {
            fieldNodes.Add(field.Field);
        }

        return new Conflict(
            string.Format(
                "Fields `{0}` conflict because they have differing arguments. "
                + "Use different aliases on the fields to fetch both if this was intentional. ",
                responseName),
            fieldNodes,
            path);
    }

    private static Conflict NameMismatchConflict(
        string fieldName1,
        string fieldName2,
        Path path,
        HashSet<FieldAndType> fields)
    {
        var fieldNodes = new HashSet<FieldNode>(SyntaxComparer.BySyntax);

        foreach (var field in fields)
        {
            fieldNodes.Add(field.Field);
        }

        return new Conflict(
            string.Format(
                "Fields `{0}` and `{1}` conflict because they have differing names. "
                + "Use different aliases on the fields to fetch both if this was intentional.",
                fieldName1,
                fieldName2),
            fieldNodes,
            path);
    }

    private sealed class FieldAndType
    {
        public FieldAndType(FieldNode field, IType? type, IType parentType)
        {
            Field = field;
            Type = type;
            ParentType = parentType;
        }

        public FieldNode Field { get; }

        public IType? Type { get; }

        public IType ParentType { get; }

        public override bool Equals(object? obj)
            => obj is FieldAndType other && Field.Equals(other.Field, SyntaxComparison.Syntax);

        public override int GetHashCode()
            => SyntaxComparer.BySyntax.GetHashCode(Field);

        public void Deconstruct(out FieldNode field, out IType? type, out IType parentType)
        {
            field = Field;
            type = Type;
            parentType = ParentType;
        }
    }

    private sealed class Conflict(string reason, HashSet<FieldNode> fields, Path? path = null)
    {
        public string Reason { get; } = reason;

        public HashSet<FieldNode> Fields { get; } = fields;

        public Path? Path { get; } = path;
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
