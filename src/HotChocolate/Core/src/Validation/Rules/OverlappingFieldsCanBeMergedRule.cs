using System.Diagnostics.CodeAnalysis;
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

    public void Validate(DocumentValidatorContext context, DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);

        ValidateInternal(new MergeContext(context), document);
    }

    private static void ValidateInternal(MergeContext context, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is not OperationDefinitionNode operationDef)
            {
                continue;
            }

            if (!context.Schema.TryGetOperationType(operationDef.Operation, out var rootType))
            {
                continue;
            }

            var fieldMap = context.RentFieldMap();
            var visitedFragmentSpreads = context.RentStringSet();
            CollectFields(context, fieldMap, operationDef.SelectionSet, rootType, visitedFragmentSpreads);

            var conflicts = context.RentConflictList();
            FindConflicts(context, fieldMap, conflicts);
            List<FieldNode>? fieldNodes = null;

            foreach (var conflict in conflicts)
            {
                if (context.ConflictsReported.Contains(conflict.Fields))
                {
                    continue;
                }

                context.ConflictsReported.Add(conflict.Fields);

                fieldNodes ??= [];
                fieldNodes.Clear();
                fieldNodes.AddRange(conflict.Fields);

                context.ReportError(
                    ErrorBuilder.New()
                        .SetMessage(conflict.Reason)
                        .AddLocations(fieldNodes.Order(FieldLocationComparer.Instance))
                        .SetPath(conflict.Path)
                        .SpecifiedBy("sec-Field-Selection-Merging")
                        .Build());
            }

            context.ReturnConflictList(conflicts);
            context.ReturnFieldMap(fieldMap);
            context.ReturnStringSet(visitedFragmentSpreads);
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
                    CollectFieldsForField(fieldMap, parentType, field, context);
                    break;

                case InlineFragmentNode inlineFragment:
                    if (inlineFragment.TypeCondition is null)
                    {
                        CollectFields(context, fieldMap, inlineFragment.SelectionSet, parentType, visitedFragmentSpreads);
                    }
                    else if (context.Schema.Types.TryGetType(inlineFragment.TypeCondition.Name.Value,
                        out var typeCondition))
                    {
                        CollectFields(context, fieldMap, inlineFragment.SelectionSet, typeCondition, visitedFragmentSpreads);
                    }
                    break;

                case FragmentSpreadNode spread:
                    if (!visitedFragmentSpreads.Add(spread.Name.Value))
                    {
                        continue;
                    }

                    if (context.Fragments.TryGet(spread, out var fragment)
                        && context.Schema.Types.TryGetType(fragment.TypeCondition.Name.Value, out var fragType))
                    {
                        CollectFields(context, fieldMap, fragment.SelectionSet, fragType, visitedFragmentSpreads);
                    }

                    break;
            }
        }
    }

    private static void CollectFieldsForField(
        Dictionary<string, HashSet<FieldAndType>> fieldMap,
        IType parentType,
        FieldNode field,
        MergeContext context)
    {
        var fieldName = field.Name.Value;
        var responseName = field.Alias?.Value ?? fieldName;
        if (!fieldMap.TryGetValue(responseName, out var fields))
        {
            fields = [];
            fieldMap[responseName] = fields;
        }

        var unwrappedParentType = parentType.NamedType();

        // __typename can be selected on unions and interfaces, which don't have this field,
        // so we need this special case for it.
        if (IntrospectionFieldNames.TypeName.Equals(fieldName, StringComparison.Ordinal))
        {
            fields.Add(new FieldAndType(field, context.TypenameFieldType, unwrappedParentType));
            return;
        }

        if (unwrappedParentType is IComplexTypeDefinition complexType
            && complexType.Fields.TryGetField(fieldName, out var fieldDef))
        {
            fields.Add(new FieldAndType(field, fieldDef.Type, complexType));
        }
    }

    private static void FindConflicts(
        MergeContext context,
        Dictionary<string, HashSet<FieldAndType>> fieldMap,
        List<Conflict> result)
    {
        SameResponseShapeByName(context, fieldMap, Path.Root, result);
        SameForCommonParentsByName(context, fieldMap, Path.Root, result);
    }

    private static void SameResponseShapeByName(
        MergeContext context,
        Dictionary<string, HashSet<FieldAndType>> fieldMap,
        Path path,
        List<Conflict> conflictsResult)
    {
        foreach (var entry in fieldMap)
        {
            if (context.SameResponseShapeChecked.Contains(entry.Value))
            {
                continue;
            }

            context.SameResponseShapeChecked.Add(entry.Value);

            var newPath = path.Append(entry.Key);
            var conflict = RequireSameOutputTypeShape(entry.Value, newPath, context);

            if (conflict != null)
            {
                conflictsResult.Add(conflict);
                continue;
            }

            var subSelections = MergeSubSelections(context, entry.Value);
            SameResponseShapeByName(context, subSelections, newPath, conflictsResult);
            context.ReturnFieldMap(subSelections);
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
                if (context.SameForCommonParentsChecked.Contains(group))
                {
                    continue;
                }

                context.SameForCommonParentsChecked.Add(group);

                var conflict = RequireSameNameAndArguments(newPath, group, context);
                if (conflict is not null)
                {
                    conflictsResult.Add(conflict);
                    continue;
                }

                conflict = RequireStreamDirectiveMergeable(context, group, newPath);
                if (conflict is not null)
                {
                    conflictsResult.Add(conflict);
                    continue;
                }

                var subSelections = MergeSubSelections(context, group);
                SameForCommonParentsByName(context, subSelections, newPath, conflictsResult);
                context.ReturnFieldMap(subSelections);
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
                    if (!concreteGroups.TryGetValue(parent, out var list))
                    {
                        list = [];
                        concreteGroups[parent] = list;
                    }

                    list.Add(field);
                    break;
            }
        }

        // If there are no concrete groups, just return one group with all abstract fields
        if (concreteGroups.Count == 0)
        {
            return [[.. abstractFields]];
        }

        var result = new List<HashSet<FieldAndType>>();

        foreach (var group in concreteGroups.Values)
        {
            var set = new HashSet<FieldAndType>();

            foreach (var field in group)
            {
                set.Add(field);
            }

            foreach (var abstractField in abstractFields)
            {
                set.Add(abstractField);
            }

            result.Add(set);
        }

        return result;
    }

    private static Conflict? RequireSameNameAndArguments(
        Path path,
        HashSet<FieldAndType> fieldGroup,
        MergeContext context)
    {
        if (fieldGroup.Count <= 1)
        {
            return null;
        }

        using var enumerator = fieldGroup.GetEnumerator();
        enumerator.MoveNext();
        var first = enumerator.Current;
        var name = first.Field.Name.Value;
        var arguments = first.Field.Arguments;
        var responseName = first.Field.Alias?.Value ?? name;

        while (enumerator.MoveNext())
        {
            var (field, _, _) = enumerator.Current;

            if (field.Name.Value != name)
            {
                return NameMismatchConflict(name, field.Name.Value, path, fieldGroup, context);
            }

            if (!SameArguments(arguments, field.Arguments))
            {
                return ArgumentMismatchConflict(responseName, path, fieldGroup, context);
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
            ArgumentNode? argB = null;

            for (var j = 0; j < b.Count; j++)
            {
                if (b[j].Name.Value.Equals(argA.Name.Value, StringComparison.Ordinal))
                {
                    argB = b[j];
                    break;
                }
            }

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
        var merged = context.RentFieldMap();
        HashSet<string>? visited = null;

        foreach (var item in fields)
        {
            if (item.Field.SelectionSet is null)
            {
                continue;
            }

            if (visited is null)
            {
                visited = context.RentStringSet();
            }
            else
            {
                visited.Clear();
            }

            CollectFields(context, merged, item.Field.SelectionSet, item.Type, visited);
        }

        if (visited is not null)
        {
            context.ReturnStringSet(visited);
        }

        return merged;
    }

    private static Conflict? RequireSameOutputTypeShape(
        HashSet<FieldAndType> fields,
        Path path,
        MergeContext context)
    {
        if (fields.Count <= 1)
        {
            return null;
        }

        using var enumerator = fields.GetEnumerator();
        enumerator.MoveNext();
        var baseField = enumerator.Current;

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
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
                            fields,
                            context);
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
                            fields,
                            context);
                    }
                }

                if (a is not (NonNullType or ListType) && b is not (NonNullType or ListType))
                {
                    break;
                }

                a = a.InnerType();
                b = b.InnerType();
            }

            if (!SameType(a, b))
            {
                return TypeMismatchConflict(
                    current.Field.Name.Value,
                    path,
                    a,
                    b,
                    fields,
                    context);
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

        if (b is null)
        {
            return false;
        }

        // if the return type of field is a union type, interface type or an object type we can merge
        // the selection set.
        if (b.Kind is TypeKind.Interface or TypeKind.Union or TypeKind.Object)
        {
            return true;
        }

        return a.Equals(b);
    }

    private static Conflict TypeMismatchConflict(
        string responseName,
        Path path,
        IType typeA,
        IType typeB,
        HashSet<FieldAndType> fields,
        MergeContext context)
    {
        var typeNameA = typeA.Print();
        var typeNameB = typeB.Print();

        return new Conflict(
            string.Format(
                "Fields `{0}` conflict because they return conflicting types `{1}` and `{2}`. "
                + "Use different aliases on the fields to fetch both if this was intentional.",
                responseName,
                typeNameA,
                typeNameB),
            GetFieldNodes(fields, context),
            path);
    }

    private static Conflict ArgumentMismatchConflict(
        string responseName,
        Path path,
        HashSet<FieldAndType> fields,
        MergeContext context)
    {
        return new Conflict(
            string.Format(
                "Fields `{0}` conflict because they have differing arguments. "
                + "Use different aliases on the fields to fetch both if this was intentional. ",
                responseName),
            GetFieldNodes(fields, context),
            path);
    }

    private static Conflict NameMismatchConflict(
        string fieldName1,
        string fieldName2,
        Path path,
        HashSet<FieldAndType> fields,
        MergeContext context)
    {
        return new Conflict(
            string.Format(
                "Fields `{0}` and `{1}` conflict because they have differing names. "
                + "Use different aliases on the fields to fetch both if this was intentional.",
                fieldName1,
                fieldName2),
            GetFieldNodes(fields, context),
            path);
    }

    private static Conflict? RequireStreamDirectiveMergeable(
        MergeContext context,
        HashSet<FieldAndType> fields,
        Path path)
    {
        // if the stream directive is disabled we can skip this check.
        if (!context.IsStreamEnabled)
        {
            return null;
        }

        FieldAndType? baseField = null;

        foreach (var f in fields)
        {
            if (HasStreamDirective(f.Field))
            {
                baseField = f;
                break;
            }
        }

        // if there is no stream directive on any field in this group we can skip this check.
        if (baseField is null)
        {
            return null;
        }

        var baseInitialCount = GetStreamInitialCount(baseField.Field);

        foreach (var (field, _, _) in fields)
        {
            if (!TryGetStreamDirective(field, out var streamDirective))
            {
                return StreamDirectiveMismatch(field.Name.Value, path, fields, context);
            }

            var initialCount = GetStreamInitialCount(streamDirective);

            if (!SyntaxComparer.BySyntax.Equals(baseInitialCount, initialCount))
            {
                return StreamDirectiveMismatch(field.Name.Value, path, fields, context);
            }
        }

        return null;
    }

    private static bool HasStreamDirective(FieldNode field)
    {
        for (var i = 0; i < field.Directives.Count; i++)
        {
            if (field.Directives[i].Name.Value.Equals(
                DirectiveNames.Stream.Name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IntValueNode? GetStreamInitialCount(FieldNode field)
    {
        for (var i = 0; i < field.Directives.Count; i++)
        {
            if (field.Directives[i].Name.Value.Equals(
                DirectiveNames.Stream.Name, StringComparison.Ordinal))
            {
                return GetStreamInitialCount(field.Directives[i]);
            }
        }

        return null;
    }

    private static bool TryGetStreamDirective(
        FieldNode field,
        [NotNullWhen(true)] out DirectiveNode? streamDirective)
    {
        for (var i = 0; i < field.Directives.Count; i++)
        {
            if (field.Directives[i].Name.Value.Equals(
                DirectiveNames.Stream.Name, StringComparison.Ordinal))
            {
                streamDirective = field.Directives[i];
                return true;
            }
        }

        streamDirective = null;
        return false;
    }

    private static IntValueNode? GetStreamInitialCount(DirectiveNode streamDirective)
    {
        for (var i = 0; i < streamDirective.Arguments.Count; i++)
        {
            if (streamDirective.Arguments[i].Name.Value.Equals(
                DirectiveNames.Stream.Arguments.InitialCount, StringComparison.Ordinal))
            {
                return streamDirective.Arguments[i].Value as IntValueNode;
            }
        }

        return null;
    }

    private static Conflict StreamDirectiveMismatch(
        string fieldName,
        Path path,
        HashSet<FieldAndType> fields,
        MergeContext context)
    {
        return new Conflict(
            string.Format(
                "Fields `{0}` conflict because they have differing stream directives. ",
                fieldName),
            GetFieldNodes(fields, context),
            path);
    }

    private static HashSet<FieldNode> GetFieldNodes(HashSet<FieldAndType> fields, MergeContext context)
    {
        var maxLocations = context.MaxLocationsPerError;
        var fieldNodes = new HashSet<FieldNode>(Math.Min(fields.Count, maxLocations));

        var i = 0;
        foreach (var field in fields)
        {
            if (i == maxLocations)
            {
                break;
            }

            fieldNodes.Add(field.Field);
            i++;
        }

        return fieldNodes;
    }

    private sealed class FieldAndType(FieldNode field, IType type, IType parentType)
    {
        public FieldNode Field { get; } = field;

        public IType Type { get; } = type;

        public IType ParentType { get; } = parentType;

        public override bool Equals(object? obj)
            => obj is FieldAndType other && Field.Equals(other.Field);

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

    private sealed class HashSetComparer<T> : IEqualityComparer<HashSet<T>> where T : notnull
    {
        public static readonly HashSetComparer<T> Instance = new();

        public bool Equals(HashSet<T>? x, HashSet<T>? y)
            => x != null && y != null && x.SetEquals(y);

        public int GetHashCode(HashSet<T> obj)
        {
            // XOR is commutative + associative, so iteration order does not matter.
            var hash = 0;

            foreach (var item in obj)
            {
                hash ^= item.GetHashCode();
            }

            return hash;
        }
    }

    private sealed class MergeContext(DocumentValidatorContext context)
    {
        private readonly Stack<Dictionary<string, HashSet<FieldAndType>>> _fieldMapPool = new();
        private readonly Stack<HashSet<string>> _stringSetPool = new();
        private readonly Stack<List<Conflict>> _conflictListPool = new();

        public ISchemaDefinition Schema => context.Schema;

        public int MaxLocationsPerError => context.MaxLocationsPerError;

        public IType TypenameFieldType { get; } = new NonNullType(context.Schema.Types["String"]);

        public HashSet<HashSet<FieldAndType>> SameResponseShapeChecked { get; } = new HashSet<HashSet<FieldAndType>>(HashSetComparer<FieldAndType>.Instance);

        public HashSet<HashSet<FieldAndType>> SameForCommonParentsChecked { get; } = new HashSet<HashSet<FieldAndType>>(HashSetComparer<FieldAndType>.Instance);

        public HashSet<HashSet<FieldNode>> ConflictsReported { get; } = new HashSet<HashSet<FieldNode>>(HashSetComparer<FieldNode>.Instance);

        public DocumentValidatorContext.FragmentContext Fragments => context.Fragments;

        public bool IsStreamEnabled { get; } = context.Schema.DirectiveDefinitions.ContainsName(DirectiveNames.Stream.Name);

        public Dictionary<string, HashSet<FieldAndType>> RentFieldMap()
        {
            if (_fieldMapPool.TryPop(out var dict))
            {
                dict.Clear();
                return dict;
            }

            return new(StringComparer.Ordinal);
        }

        public void ReturnFieldMap(Dictionary<string, HashSet<FieldAndType>> dict)
            => _fieldMapPool.Push(dict);

        public HashSet<string> RentStringSet()
        {
            if (_stringSetPool.TryPop(out var set))
            {
                set.Clear();
                return set;
            }

            return new(StringComparer.Ordinal);
        }

        public void ReturnStringSet(HashSet<string> set)
            => _stringSetPool.Push(set);

        public List<Conflict> RentConflictList()
        {
            if (_conflictListPool.TryPop(out var list))
            {
                list.Clear();
                return list;
            }

            return [];
        }

        public void ReturnConflictList(List<Conflict> list)
            => _conflictListPool.Push(list);

        public void ReportError(IError error)
        {
            context.ReportError(error);
        }
    }

    private sealed class FieldLocationComparer : IComparer<FieldNode>
    {
        public static FieldLocationComparer Instance { get; } = new();

        public int Compare(FieldNode? x, FieldNode? y)
        {
            if (x is null && y is null)
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var xLocation = x.Location;
            var yLocation = y.Location;

            if (xLocation is null && yLocation is null)
            {
                return 0;
            }

            if (xLocation is null)
            {
                return -1;
            }

            if (yLocation is null)
            {
                return 1;
            }

            return xLocation.CompareTo(yLocation);
        }
    }
}
