using HotChocolate.Language;

namespace HotChocolate.Fusion;

/// <summary>
/// Normalizes field selections such as the <c>fields</c> argument of <c>@key</c>, so that
/// selections which differ only in whitespace, ordering, repeated selections, or redundant inline
/// fragments compare equal.
/// </summary>
internal static class FieldSelection
{
    /// <summary>
    /// Parses a field selection given without the outer braces and returns its normalized form in
    /// the same notation.
    /// </summary>
    /// <exception cref="SyntaxException">
    /// The <paramref name="fields"/> value is not a valid selection set.
    /// </exception>
    public static string Normalize(string fields)
    {
        var selectionSet = Normalize(Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{ {fields} }}"));
        return string.Join(" ", selectionSet.Selections.Select(s => s.ToString(false)));
    }

    /// <summary>
    /// Returns the normalized form of a selection set: selections that GraphQL field merging treats
    /// as equal are merged, inline fragments without a type condition or repeating the enclosing
    /// fragment's type condition are inlined, and arguments and selections are ordered at every
    /// nesting level.
    /// </summary>
    public static SelectionSetNode Normalize(SelectionSetNode selectionSet)
    {
        var level = new Level(typeCondition: null);
        level.Collect(selectionSet);
        return level.Build();
    }

    private static FieldNode SortArguments(FieldNode field)
    {
        var arguments = field.Arguments;

        if (arguments.Count < 2)
        {
            return field;
        }

        var isSorted = true;

        for (var i = 1; i < arguments.Count; i++)
        {
            if (string.CompareOrdinal(arguments[i - 1].Name.Value, arguments[i].Name.Value) > 0)
            {
                isSorted = false;
                break;
            }
        }

        if (isSorted)
        {
            return field;
        }

        var sorted = arguments.ToArray();
        Array.Sort(sorted, static (a, b) => string.CompareOrdinal(a.Name.Value, b.Name.Value));
        return field.WithArguments(sorted);
    }

    /// <summary>
    /// Collects the selections of one nesting level, grouped for merging. The type condition is the
    /// one of the enclosing inline fragment, or null when it is unknown.
    /// </summary>
    private sealed class Level(string? typeCondition)
    {
        private Dictionary<string, List<FieldNode>>? _fieldsByResponseName;
        private List<InlineFragmentNode>? _inlineFragments;
        private List<FragmentSpreadNode>? _fragmentSpreads;

        public void Collect(SelectionSetNode selectionSet)
        {
            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field:
                        AddField(SortArguments(field));
                        break;

                    case InlineFragmentNode inlineFragment:
                        if (IsRedundant(inlineFragment))
                        {
                            Collect(inlineFragment.SelectionSet);
                        }
                        else
                        {
                            _inlineFragments ??= [];
                            _inlineFragments.Add(inlineFragment);
                        }

                        break;

                    case FragmentSpreadNode fragmentSpread:
                        _fragmentSpreads ??= [];
                        _fragmentSpreads.Add(fragmentSpread);
                        break;
                }
            }
        }

        public SelectionSetNode Build()
        {
            var selections = new List<ISelectionNode>();

            if (_fieldsByResponseName is not null)
            {
                foreach (var fields in _fieldsByResponseName.Values)
                {
                    foreach (var group in fields.GroupBy(f => f, FieldComparer.Instance))
                    {
                        selections.Add(MergeFields(group));
                    }
                }
            }

            if (_inlineFragments is not null)
            {
                foreach (var group in _inlineFragments.GroupBy(f => f, InlineFragmentComparer.Instance))
                {
                    selections.Add(MergeInlineFragments(group));
                }
            }

            if (_fragmentSpreads is not null)
            {
                var seen = new HashSet<ISyntaxNode>(SyntaxComparer.BySyntax);

                foreach (var fragmentSpread in _fragmentSpreads)
                {
                    if (seen.Add(fragmentSpread))
                    {
                        selections.Add(fragmentSpread);
                    }
                }
            }

            selections.Sort(SelectionComparer.Instance);

            return new SelectionSetNode(selections);
        }

        private void AddField(FieldNode field)
        {
            _fieldsByResponseName ??= new(StringComparer.Ordinal);
            var responseName = field.Alias?.Value ?? field.Name.Value;

            if (!_fieldsByResponseName.TryGetValue(responseName, out var fields))
            {
                fields = [];
                _fieldsByResponseName.Add(responseName, fields);
            }

            fields.Add(field);
        }

        private bool IsRedundant(InlineFragmentNode inlineFragment)
            => inlineFragment.Directives.Count == 0
                && (inlineFragment.TypeCondition is null
                    || inlineFragment.TypeCondition.Name.Value.Equals(typeCondition, StringComparison.Ordinal));

        private static FieldNode MergeFields(IGrouping<FieldNode, FieldNode> group)
        {
            var first = group.Key;
            Level? level = null;

            foreach (var field in group)
            {
                if (field.SelectionSet is null)
                {
                    continue;
                }

                level ??= new Level(typeCondition: null);
                level.Collect(field.SelectionSet);
            }

            return level is null ? first : first.WithSelectionSet(level.Build());
        }

        private InlineFragmentNode MergeInlineFragments(
            IGrouping<InlineFragmentNode, InlineFragmentNode> group)
        {
            var level = new Level(group.Key.TypeCondition?.Name.Value ?? typeCondition);

            foreach (var inlineFragment in group)
            {
                level.Collect(inlineFragment.SelectionSet);
            }

            return group.Key.WithSelectionSet(level.Build());
        }
    }

    /// <summary>
    /// Compares fields by everything but their selection set, which is how field merging decides
    /// whether two fields are the same field.
    /// </summary>
    private sealed class FieldComparer : IEqualityComparer<FieldNode>
    {
        public static FieldComparer Instance { get; } = new();

        public bool Equals(FieldNode? x, FieldNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return SyntaxComparer.BySyntax.Equals(x.Alias, y.Alias)
                && x.Name.Equals(y.Name)
                && x.Directives.SequenceEqual(y.Directives, SyntaxComparer.BySyntax)
                && x.Arguments.SequenceEqual(y.Arguments, SyntaxComparer.BySyntax);
        }

        public int GetHashCode(FieldNode obj)
        {
            var hashCode = new HashCode();

            if (obj.Alias is not null)
            {
                hashCode.Add(obj.Alias.Value);
            }

            hashCode.Add(obj.Name.Value);

            foreach (var directive in obj.Directives)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(directive));
            }

            foreach (var argument in obj.Arguments)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(argument));
            }

            return hashCode.ToHashCode();
        }
    }

    /// <summary>
    /// Compares inline fragments by type condition and directives, ignoring their selection set.
    /// </summary>
    private sealed class InlineFragmentComparer : IEqualityComparer<InlineFragmentNode>
    {
        public static InlineFragmentComparer Instance { get; } = new();

        public bool Equals(InlineFragmentNode? x, InlineFragmentNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return SyntaxComparer.BySyntax.Equals(x.TypeCondition, y.TypeCondition)
                && x.Directives.SequenceEqual(y.Directives, SyntaxComparer.BySyntax);
        }

        public int GetHashCode(InlineFragmentNode obj)
        {
            var hashCode = new HashCode();

            if (obj.TypeCondition is not null)
            {
                hashCode.Add(obj.TypeCondition.Name.Value);
            }

            foreach (var directive in obj.Directives)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(directive));
            }

            return hashCode.ToHashCode();
        }
    }

    /// <summary>
    /// Orders selections by kind, then by name, and only falls back to their printed form when two
    /// selections of the same name differ in arguments or directives.
    /// </summary>
    private sealed class SelectionComparer : IComparer<ISelectionNode>
    {
        public static SelectionComparer Instance { get; } = new();

        public int Compare(ISelectionNode? x, ISelectionNode? y)
        {
            if (ReferenceEquals(x, y))
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

            var rank = GetRank(x).CompareTo(GetRank(y));

            if (rank != 0)
            {
                return rank;
            }

            var name = string.CompareOrdinal(GetName(x), GetName(y));

            if (name != 0)
            {
                return name;
            }

            return string.CompareOrdinal(x.ToString(false), y.ToString(false));
        }

        private static int GetRank(ISelectionNode selection)
            => selection switch
            {
                InlineFragmentNode => 0,
                FragmentSpreadNode => 1,
                _ => 2
            };

        private static string GetName(ISelectionNode selection)
            => selection switch
            {
                FieldNode field => field.Alias?.Value ?? field.Name.Value,
                InlineFragmentNode inlineFragment => inlineFragment.TypeCondition?.Name.Value ?? string.Empty,
                FragmentSpreadNode fragmentSpread => fragmentSpread.Name.Value,
                _ => string.Empty
            };
    }
}
