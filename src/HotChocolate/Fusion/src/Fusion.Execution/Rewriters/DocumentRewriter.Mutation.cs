using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Rewriters;

public sealed partial class DocumentRewriter
{
    private const int MaxMutationFieldConditions = 1024;

    /// <summary>
    /// Rewrites the root selection set of a mutation into its root field occurrences in document order,
    /// each conditioned to be included only when it is the first included occurrence of its response name.
    /// An occurrence also selects the sub-selections of the later occurrences that merge into it.
    /// </summary>
    private SelectionSetNode RewriteMutationSelectionSet(
        SelectionSetNode selectionSetNode,
        ITypeDefinition mutationType,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
    {
        var context = new Context(null, null, null, mutationType, null, null, fragmentLookup);
        var occurrences = new List<MutationFieldOccurrence>();

        // Root fragments that carry a @defer or another executable directive keep the general rewrite.
        if (!TryCollectMutationFields(selectionSetNode, context, [], occurrences))
        {
            return RewriteSelectionSet(selectionSetNode, mutationType, fragmentLookup);
        }

        var variants = CreateMutationFieldVariants(occurrences);

        // A sub-selection with a @defer that would be copied into several root selections keeps the general rewrite.
        if (HasRepeatedDeferredSelection(occurrences, variants, fragmentLookup))
        {
            return RewriteSelectionSet(selectionSetNode, mutationType, fragmentLookup);
        }

        List<ISelectionNode>? selections = null;

        foreach (var variant in variants)
        {
            var selection = RewriteMutationField(occurrences, variant, mutationType, fragmentLookup);

            if (selection is not null)
            {
                selections ??= [];
                selections.Add(selection);
            }
        }

        return new SelectionSetNode(selections ?? [s_typeNameField]);
    }

    private bool TryCollectMutationFields(
        SelectionSetNode selectionSet,
        Context context,
        MutationFieldCondition[] conditions,
        List<MutationFieldOccurrence> occurrences)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (removeStaticallyExcludedSelections && IsStaticallySkipped(selection))
            {
                continue;
            }

            switch (selection)
            {
                case FieldNode field:
                    if (!TryCollectMutationField(field, context, conditions, occurrences))
                    {
                        return false;
                    }

                    break;

                case InlineFragmentNode inlineFragment:
                    if (inlineFragment.TypeCondition is not null)
                    {
                        var typeConditionName = inlineFragment.TypeCondition.Name.Value;

                        if (!schema.Types.TryGetType(
                            typeConditionName,
                            allowInaccessibleFields: true,
                            out var typeCondition))
                        {
                            throw ThrowHelper.InvalidTypeConditionOnInlineFragment(
                                inlineFragment,
                                context.Type.Name);
                        }

                        if (!typeCondition.IsAssignableFrom(context.Type))
                        {
                            return false;
                        }
                    }

                    if (!TryCollectMutationFragment(
                        inlineFragment,
                        inlineFragment.SelectionSet,
                        context,
                        conditions,
                        occurrences))
                    {
                        return false;
                    }

                    break;

                case FragmentSpreadNode fragmentSpread:
                    var fragmentDefinition = context.GetFragmentDefinition(fragmentSpread);
                    var fragmentTypeName = fragmentDefinition.TypeCondition.Name.Value;

                    if (!schema.Types.TryGetType(
                        fragmentTypeName,
                        allowInaccessibleFields: true,
                        out var fragmentType))
                    {
                        throw ThrowHelper.InvalidTypeConditionOnFragment(fragmentSpread, fragmentTypeName);
                    }

                    if (!fragmentType.IsAssignableFrom(context.Type)
                        || !TryCollectMutationFragment(
                            fragmentSpread,
                            fragmentDefinition.SelectionSet,
                            context,
                            conditions,
                            occurrences))
                    {
                        return false;
                    }

                    break;
            }
        }

        return true;
    }

    private bool TryCollectMutationFragment(
        IHasDirectives fragment,
        SelectionSetNode selectionSet,
        Context context,
        MutationFieldCondition[] conditions,
        List<MutationFieldOccurrence> occurrences)
    {
        var (conditional, defer, directives) = DivideDirectives(
            fragment,
            HotChocolate.Types.DirectiveLocation.InlineFragment);

        if (defer is not null || directives is not null)
        {
            return false;
        }

        if (!TryAddConditions(conditions, conditional, out var fragmentConditions))
        {
            return false;
        }

        // A fragment whose conditions contradict each other is never included.
        if (fragmentConditions is null)
        {
            return true;
        }

        return TryCollectMutationFields(selectionSet, context, fragmentConditions, occurrences);
    }

    private bool TryCollectMutationField(
        FieldNode fieldNode,
        Context context,
        MutationFieldCondition[] conditions,
        List<MutationFieldOccurrence> occurrences)
    {
        var (conditional, _, directives) = DivideDirectives(
            fieldNode,
            HotChocolate.Types.DirectiveLocation.Field);

        if (!TryAddConditions(conditions, conditional, out var fieldConditions))
        {
            return false;
        }

        ITypeDefinition? fieldType = null;

        if (fieldNode.SelectionSet is not null && context.Type is IComplexTypeDefinition complexType)
        {
            if (!complexType.Fields.TryGetField(fieldNode.Name.Value, out var field))
            {
                throw ThrowHelper.FieldDoesNotExistOnType(fieldNode, complexType.Name);
            }

            fieldType = field.Type.AsTypeDefinition();
        }

        // A field whose conditions contradict each other is never included.
        if (fieldConditions is null)
        {
            return true;
        }

        fieldNode = new FieldNode(
            fieldNode.Name,
            fieldNode.Alias,
            directives ?? [],
            RewriteArguments(fieldNode.Arguments),
            fieldNode.SelectionSet);

        occurrences.Add(new MutationFieldOccurrence(fieldNode, fieldType, fieldConditions));
        return true;
    }

    /// <summary>
    /// Appends the variable conditions of <paramref name="conditional"/> to <paramref name="conditions"/>.
    /// Returns <c>false</c> when a condition is not a variable and <paramref name="combined"/> is
    /// <c>null</c> when the combined conditions contradict each other.
    /// </summary>
    private static bool TryAddConditions(
        MutationFieldCondition[] conditions,
        Conditional? conditional,
        out MutationFieldCondition[]? combined)
    {
        combined = conditions;

        if (conditional is null)
        {
            return true;
        }

        var list = new List<MutationFieldCondition>(conditions);

        foreach (var directive in conditional.ToDirectives())
        {
            if (directive.Arguments is not [{ Value: VariableNode variable }])
            {
                return false;
            }

            var passingValue = directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal);
            var condition = new MutationFieldCondition(variable.Name.Value, passingValue);

            if (list.Contains(condition.Negate()))
            {
                combined = null;
                return true;
            }

            if (!list.Contains(condition))
            {
                list.Add(condition);
            }
        }

        combined = [.. list];
        return true;
    }

    /// <summary>
    /// Creates the root selections of the occurrences in document order, each with the later
    /// occurrences of its response name that merge into it.
    /// </summary>
    private static List<MutationFieldVariant> CreateMutationFieldVariants(
        List<MutationFieldOccurrence> occurrences)
    {
        var variants = new List<MutationFieldVariant>();
        var conditionCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < occurrences.Count; index++)
        {
            var occurrence = occurrences[index];
            conditionCounts.TryGetValue(occurrence.ResponseName, out var conditionCount);

            foreach (var conditions in CreateMutationFieldConditionSets(occurrences, index, conditionCount))
            {
                var variant = new MutationFieldVariant(index, conditions);
                conditionCount += conditions.Length;

                if (occurrence.Field.SelectionSet is not null)
                {
                    // Later occurrences of the response name merge into this one while their own
                    // conditions are met.
                    for (var i = index + 1; i < occurrences.Count; i++)
                    {
                        var later = occurrences[i];

                        if (later.Field.SelectionSet is null
                            || !later.ResponseName.Equals(occurrence.ResponseName, StringComparison.Ordinal)
                            || !TryGetRemainingConditions(later.Conditions, conditions, out var remaining))
                        {
                            continue;
                        }

                        variant.AddMergedOccurrence(i, remaining);
                        conditionCount += remaining.Length;
                    }
                }

                if (conditionCount > MaxMutationFieldConditions)
                {
                    throw ThrowHelper.MutationFieldHasTooManyConditionVariants(
                        occurrence.Field,
                        MaxMutationFieldConditions);
                }

                variants.Add(variant);
            }

            conditionCounts[occurrence.ResponseName] = conditionCount;
        }

        return variants;
    }

    /// <summary>
    /// Creates the mutually exclusive condition sets under which the occurrence at
    /// <paramref name="index"/> is included while no earlier occurrence of its response name is.
    /// <paramref name="conditionCount"/> is the number of conditions the earlier occurrences already emit.
    /// </summary>
    private static List<MutationFieldCondition[]> CreateMutationFieldConditionSets(
        List<MutationFieldOccurrence> occurrences,
        int index,
        int conditionCount)
    {
        var occurrence = occurrences[index];
        List<MutationFieldCondition[]> variants = [occurrence.Conditions];

        for (var i = 0; i < index && variants.Count > 0; i++)
        {
            var earlier = occurrences[i];

            if (!earlier.ResponseName.Equals(occurrence.ResponseName, StringComparison.Ordinal))
            {
                continue;
            }

            var next = new List<MutationFieldCondition[]>();
            var nextConditionCount = conditionCount;

            foreach (var variant in variants)
            {
                if (!TryGetRemainingConditions(earlier.Conditions, variant, out var remaining))
                {
                    // The earlier occurrence is excluded whenever this variant applies.
                    next.Add(variant);
                    nextConditionCount += variant.Length;
                    continue;
                }

                // The variant implies the earlier occurrence, so it is never the first one.
                if (remaining.Length == 0)
                {
                    continue;
                }

                // The earlier occurrence is excluded when any of its remaining conditions fails.
                // Each split negates one condition and keeps the ones before it, so the splits
                // never apply at the same time.
                var prefix = variant;

                foreach (var condition in remaining)
                {
                    MutationFieldCondition[] split = [.. prefix, condition.Negate()];
                    next.Add(split);
                    nextConditionCount += split.Length;
                    prefix = [.. prefix, condition];
                }

                if (nextConditionCount > MaxMutationFieldConditions)
                {
                    throw ThrowHelper.MutationFieldHasTooManyConditionVariants(
                        occurrence.Field,
                        MaxMutationFieldConditions);
                }
            }

            variants = next;
        }

        return variants;
    }

    /// <summary>
    /// Returns <c>true</c> when the sub-selection of an occurrence contains a <c>@defer</c>
    /// and is copied into more than one root selection.
    /// </summary>
    private static bool HasRepeatedDeferredSelection(
        List<MutationFieldOccurrence> occurrences,
        List<MutationFieldVariant> variants,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
    {
        var copies = new int[occurrences.Count];

        foreach (var variant in variants)
        {
            copies[variant.OccurrenceIndex]++;

            if (variant.MergedOccurrences is not null)
            {
                foreach (var merged in variant.MergedOccurrences)
                {
                    copies[merged.OccurrenceIndex]++;
                }
            }
        }

        HashSet<string>? visitedFragments = null;

        for (var i = 0; i < occurrences.Count; i++)
        {
            if (copies[i] > 1
                && occurrences[i].Field.SelectionSet is { } selectionSet
                && ContainsDefer(selectionSet, fragmentLookup, visitedFragments ??= []))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsDefer(
        SelectionSetNode selectionSet,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup,
        HashSet<string> visitedFragments)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode { SelectionSet: { } fieldSelectionSet }:
                    if (ContainsDefer(fieldSelectionSet, fragmentLookup, visitedFragments))
                    {
                        return true;
                    }

                    break;

                case InlineFragmentNode inlineFragment:
                    if (HasDeferDirective(inlineFragment)
                        || ContainsDefer(inlineFragment.SelectionSet, fragmentLookup, visitedFragments))
                    {
                        return true;
                    }

                    break;

                case FragmentSpreadNode fragmentSpread:
                    if (HasDeferDirective(fragmentSpread))
                    {
                        return true;
                    }

                    // A fragment that was visited before contains no @defer.
                    if (visitedFragments.Add(fragmentSpread.Name.Value)
                        && fragmentLookup.TryGetValue(fragmentSpread.Name.Value, out var fragmentDefinition)
                        && ContainsDefer(fragmentDefinition.SelectionSet, fragmentLookup, visitedFragments))
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;

        static bool HasDeferDirective(IHasDirectives fragment)
        {
            foreach (var directive in fragment.Directives)
            {
                if (directive.Name.Value.Equals(DirectiveNames.Defer.Name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Removes the conditions that <paramref name="applied"/> already guarantees.
    /// Returns <c>false</c> when <paramref name="applied"/> contradicts one of the conditions.
    /// </summary>
    private static bool TryGetRemainingConditions(
        MutationFieldCondition[] conditions,
        MutationFieldCondition[] applied,
        out MutationFieldCondition[] remaining)
    {
        List<MutationFieldCondition>? list = null;

        foreach (var condition in conditions)
        {
            if (Array.IndexOf(applied, condition.Negate()) >= 0)
            {
                remaining = [];
                return false;
            }

            if (Array.IndexOf(applied, condition) < 0)
            {
                list ??= [];
                list.Add(condition);
            }
        }

        remaining = list is null ? [] : [.. list];
        return true;
    }

    private ISelectionNode? RewriteMutationField(
        List<MutationFieldOccurrence> occurrences,
        MutationFieldVariant variant,
        ITypeDefinition mutationType,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
    {
        var occurrence = occurrences[variant.OccurrenceIndex];
        var fieldNode = occurrence.Field;

        if (fieldNode.SelectionSet is not null)
        {
            var context = new Context(null, null, null, mutationType, null, null, fragmentLookup);
            var fieldContext = context.AddField(fieldNode, occurrence.FieldType);

            if (fieldContext is null)
            {
                return null;
            }

            CollectSelections(fieldNode.SelectionSet, fieldContext);

            if (variant.MergedOccurrences is not null)
            {
                foreach (var merged in variant.MergedOccurrences)
                {
                    var later = occurrences[merged.OccurrenceIndex];
                    CollectSelections(WrapInConditions(later.Field.SelectionSet!, merged.Conditions), fieldContext);
                }
            }

            var rewrittenField = RewriteField(fieldNode, fieldContext);

            if (rewrittenField is null)
            {
                return null;
            }

            fieldNode = rewrittenField;
        }

        var groups = GroupConditionDirectives(variant.Conditions);

        if (groups.Count == 0)
        {
            return fieldNode;
        }

        ISelectionNode selection;
        var outerGroupCount = groups.Count;

        if (fieldNode.Directives.Count == 0)
        {
            outerGroupCount--;
            selection = fieldNode.WithDirectives(groups[outerGroupCount]);
        }
        else
        {
            selection = fieldNode;
        }

        for (var i = outerGroupCount - 1; i >= 0; i--)
        {
            selection = new InlineFragmentNode(null, null, groups[i], new SelectionSetNode([selection]));
        }

        return selection;
    }

    private static SelectionSetNode WrapInConditions(
        SelectionSetNode selectionSet,
        MutationFieldCondition[] conditions)
    {
        var groups = GroupConditionDirectives(conditions);

        for (var i = groups.Count - 1; i >= 0; i--)
        {
            selectionSet = new SelectionSetNode([new InlineFragmentNode(null, null, groups[i], selectionSet)]);
        }

        return selectionSet;
    }

    /// <summary>
    /// Groups the conditions into directive lists with at most one <c>@skip</c> and one <c>@include</c> each.
    /// </summary>
    private static List<DirectiveNode[]> GroupConditionDirectives(MutationFieldCondition[] conditions)
    {
        var groups = new List<DirectiveNode[]>();
        DirectiveNode? skip = null;
        DirectiveNode? include = null;

        foreach (var condition in conditions)
        {
            if ((condition.PassingValue ? include : skip) is not null)
            {
                groups.Add(CreateGroup(skip, include));
                skip = null;
                include = null;
            }

            var directive = new DirectiveNode(
                condition.PassingValue ? DirectiveNames.Include.Name : DirectiveNames.Skip.Name,
                new ArgumentNode(
                    DirectiveNames.Include.Arguments.If,
                    new VariableNode(condition.VariableName)));

            if (condition.PassingValue)
            {
                include = directive;
            }
            else
            {
                skip = directive;
            }
        }

        if (skip is not null || include is not null)
        {
            groups.Add(CreateGroup(skip, include));
        }

        return groups;

        static DirectiveNode[] CreateGroup(DirectiveNode? skip, DirectiveNode? include)
            => (skip, include) switch
            {
                ({ } s, { } i) => [s, i],
                ({ } s, null) => [s],
                (null, { } i) => [i],
                _ => []
            };
    }

    private sealed class MutationFieldOccurrence(
        FieldNode field,
        ITypeDefinition? fieldType,
        MutationFieldCondition[] conditions)
    {
        public FieldNode Field { get; } = field;

        public ITypeDefinition? FieldType { get; } = fieldType;

        public MutationFieldCondition[] Conditions { get; } = conditions;

        public string ResponseName { get; } = field.Alias?.Value ?? field.Name.Value;
    }

    private sealed class MutationFieldVariant(int occurrenceIndex, MutationFieldCondition[] conditions)
    {
        public int OccurrenceIndex { get; } = occurrenceIndex;

        public MutationFieldCondition[] Conditions { get; } = conditions;

        /// <summary>
        /// Gets the later occurrences whose sub-selections merge into this root selection,
        /// each with the conditions it keeps, or <c>null</c> when none merge.
        /// </summary>
        public List<MergedMutationField>? MergedOccurrences { get; private set; }

        public void AddMergedOccurrence(int occurrenceIndex, MutationFieldCondition[] conditions)
            => (MergedOccurrences ??= []).Add(new MergedMutationField(occurrenceIndex, conditions));
    }

    private readonly record struct MergedMutationField(int OccurrenceIndex, MutationFieldCondition[] Conditions);

    private readonly record struct MutationFieldCondition(string VariableName, bool PassingValue)
    {
        public MutationFieldCondition Negate() => this with { PassingValue = !PassingValue };
    }
}
