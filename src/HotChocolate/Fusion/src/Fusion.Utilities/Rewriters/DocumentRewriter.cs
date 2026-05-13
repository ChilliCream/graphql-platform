using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed class DocumentRewriter(
    ISchemaDefinition schema,
    bool removeStaticallyExcludedSelections = false,
    bool includeTypeNameToEmptySelectionSets = true)
{
    private bool _hasIncrementalParts;

    private static readonly FieldNode s_typeNameField =
        new FieldNode(
            null,
            new NameNode(IntrospectionFieldNames.TypeName),
            null,
            [new DirectiveNode("fusion__empty")],
            ImmutableArray<ArgumentNode>.Empty,
            null);

    /// <summary>
    /// Rewrites a GraphQL document by normalizing conditional directives, ordering
    /// selections by spec-mandated depth-first first-occurrence, and folding adjacent
    /// same-key conditional/unconditional sibling pairs where safe.
    /// </summary>
    /// <param name="document">The GraphQL document to rewrite.</param>
    /// <param name="operationName">
    /// The name of the operation to rewrite. If <c>null</c>, the first or only operation in the document is used.
    /// </param>
    /// <returns>
    /// A result containing the rewritten document and a flag indicating whether the document
    /// contains <c>@defer</c> or <c>@stream</c> directives for incremental delivery.
    /// </returns>
    public DocumentRewriterResult RewriteDocument(DocumentNode document, string? operationName = null)
    {
        _hasIncrementalParts = false;

        var operation = document.GetOperation(operationName);
        var operationType = schema.GetOperationType(operation.Operation);
        var fragmentLookup = CreateFragmentLookup(document);

        var newSelectionSet = RewriteSelectionSet(
            operation.SelectionSet,
            operationType,
            fragmentLookup);

        var newOperation = new OperationDefinitionNode(
            null,
            operation.Name,
            operation.Description,
            operation.Operation,
            operation.VariableDefinitions,
            RewriteDirectives(operation.Directives),
            newSelectionSet);

        return new DocumentRewriterResult(new DocumentNode([newOperation]), _hasIncrementalParts);
    }

    private SelectionSetNode RewriteSelectionSet(
        SelectionSetNode selectionSetNode,
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode>? fragmentLookup)
    {
        var context = new Context(null, null, type, null, fragmentLookup ?? []);

        CollectSelections(selectionSetNode, context);

        var newSelections = RewriteSelections(context)
            ?? (includeTypeNameToEmptySelectionSets ? [s_typeNameField] : (List<ISelectionNode>)[]);

        var newSelectionSetNode = new SelectionSetNode(newSelections);

        return newSelectionSetNode;
    }

    #region Collecting

    private void CollectSelections(SelectionSetNode selectionSet, Context context)
    {
        CollectSelectionsCore(selectionSet, context);

        RemoveSubsumedDeferConditionals(context);
        PruneRedundantConditionalSubFields(context);
        FoldAdjacentSameKeyConditionals(context);
    }

    /// <summary>
    /// Iterates a selection set into <paramref name="context"/> without running the
    /// post-collection passes. Used for inlined fragments that contribute selections to an
    /// outer context still being collected, so the post-passes run once after all sibling
    /// selections have been added rather than mid-iteration.
    /// </summary>
    private void CollectSelectionsCore(SelectionSetNode selectionSet, Context context)
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
                    CollectField(field, context);
                    break;

                case InlineFragmentNode inlineFragment:
                    CollectInlineFragment(inlineFragment, context);
                    break;

                case FragmentSpreadNode fragmentSpread:
                    CollectFragmentSpread(fragmentSpread, context);
                    break;
            }
        }
    }

    /// <summary>
    /// A field or fragment selection inside a <c>... @defer</c> is subsumed when the same
    /// selection also appears outside of any @defer at the same level (either unconditionally,
    /// or under @skip / @include). In that case the deferred version is redundant and removed.
    /// </summary>
    private static void RemoveSubsumedDeferConditionals(Context context)
    {
        if (context.Conditionals is null)
        {
            return;
        }

        List<Conditional>? deferOnlyKeys = null;

        foreach (var (cond, _) in context.Conditionals)
        {
            if (cond.IsDeferOnly)
            {
                deferOnlyKeys ??= [];
                deferOnlyKeys.Add(cond);
            }
        }

        if (deferOnlyKeys is null)
        {
            return;
        }

        foreach (var deferKey in deferOnlyKeys)
        {
            var deferCtx = context.Conditionals[deferKey];

            if (deferCtx.Fields is not null)
            {
                List<FieldNode>? fieldsToRemove = null;

                foreach (var (_, fieldDict) in deferCtx.Fields)
                {
                    foreach (var (fieldNode, deferredFieldContext) in fieldDict)
                    {
                        if (TrySubsumeFieldIntoNonDeferSibling(
                            context,
                            deferKey,
                            fieldNode,
                            deferredFieldContext))
                        {
                            fieldsToRemove ??= [];
                            fieldsToRemove.Add(fieldNode);
                        }
                    }
                }

                if (fieldsToRemove is not null)
                {
                    foreach (var f in fieldsToRemove)
                    {
                        deferCtx.RemoveField(f);
                    }
                }
            }

            if (deferCtx.Fragments is not null)
            {
                List<InlineFragmentNode>? fragmentsToRemove = null;

                foreach (var (_, fragmentDict) in deferCtx.Fragments)
                {
                    foreach (var (fragmentNode, deferredFragmentContext) in fragmentDict)
                    {
                        if (TrySubsumeFragmentIntoNonDeferSibling(
                            context,
                            deferKey,
                            fragmentNode,
                            deferredFragmentContext))
                        {
                            fragmentsToRemove ??= [];
                            fragmentsToRemove.Add(fragmentNode);
                        }
                    }
                }

                if (fragmentsToRemove is not null)
                {
                    foreach (var f in fragmentsToRemove)
                    {
                        deferCtx.RemoveFragment(f);
                    }
                }
            }

            if (IsContextEmpty(deferCtx))
            {
                context.Conditionals.Remove(deferKey);
            }
        }
    }

    private static bool TrySubsumeFieldIntoNonDeferSibling(
        Context level,
        Conditional deferKey,
        FieldNode fieldNode,
        Context? deferredFieldContext)
    {
        // Subsumption is only safe when the same field is selected unconditionally at
        // this level. An unconditional selection guarantees the field is always part of
        // the initial payload, so the deferred occurrence cannot change when the field
        // is first delivered. A field selected under @skip / @include has a different
        // delivery condition than a deferred occurrence, so the two cannot be merged
        // without changing whether or when the field is delivered.
        if (!level.HasField(fieldNode, out var unconditionalFieldContext))
        {
            return false;
        }

        // The unconditional sibling must appear before the deferred occurrence textually.
        // If the deferred occurrence appears first, dropping it would change the response
        // field position from inside the defer payload to the initial payload position
        // owned by the later unconditional sibling, which is not the spec-mandated order.
        var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;
        var unconditionalSlotIndex = level.IndexOfSlot(SlotKind.Field, responseName);
        var deferSlotIndex = level.IndexOfSlot(SlotKind.Conditional, deferKey);

        if (unconditionalSlotIndex < 0 || deferSlotIndex < 0 || unconditionalSlotIndex > deferSlotIndex)
        {
            return false;
        }

        if (deferredFieldContext is not null && unconditionalFieldContext is not null)
        {
            // Composite field: lift the deferred sub-selections under the unconditional
            // field as a defer sub-conditional. The merge naturally drops any fields
            // that already exist unconditionally there, leaving only the unique ones
            // deferred.
            var subDeferContext = unconditionalFieldContext.GetOrAddConditionalContext(deferKey);
            MergeContexts(deferredFieldContext, subDeferContext);
        }

        return true;
    }

    private static bool TrySubsumeFragmentIntoNonDeferSibling(
        Context level,
        Conditional deferKey,
        InlineFragmentNode fragmentNode,
        Context deferredFragmentContext)
    {
        // Same rule as for fields: only subsume when the same fragment is selected
        // unconditionally at this level.
        if (!level.HasFragment(fragmentNode, out var subsumingFragmentContext))
        {
            return false;
        }

        var unconditionalSlotIndex = level.IndexOfSlot(SlotKind.Fragment, fragmentNode);
        var deferSlotIndex = level.IndexOfSlot(SlotKind.Conditional, deferKey);

        if (unconditionalSlotIndex < 0 || deferSlotIndex < 0 || unconditionalSlotIndex > deferSlotIndex)
        {
            return false;
        }

        var subDeferContext = subsumingFragmentContext.GetOrAddConditionalContext(deferKey);
        MergeContexts(deferredFragmentContext, subDeferContext);

        return true;
    }

    private static bool IsContextEmpty(Context context)
    {
        if (context.Fields is not null)
        {
            foreach (var (_, dict) in context.Fields)
            {
                if (dict.Count > 0)
                {
                    return false;
                }
            }
        }

        if (context.Fragments is not null)
        {
            foreach (var (_, dict) in context.Fragments)
            {
                if (dict.Count > 0)
                {
                    return false;
                }
            }
        }

        if (context.Conditionals?.Count > 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// When a conditional sibling at a level is wholesale-redundant (every direct selection
    /// is already delivered by an unconditional sibling, in the same relative textual order)
    /// the conditional is dropped entirely.
    /// <br/>
    /// Partial pruning is not safe: dropping individual overlapping sub-fields shifts the
    /// remaining sub-fields' first-occurrence positions in the merged response, which
    /// violates the depth-first first-occurrence ordering mandated by the GraphQL spec
    /// (Section 6, Execution: <c>CollectFields</c> order is preserved through execution).
    /// <br/>
    /// For <c>@defer</c>-only conditionals the cover slots must additionally appear before
    /// the defer slot. A trailing unconditional sibling at a later position cannot subsume
    /// the deferred occurrence without changing the response field position from inside the
    /// deferred patch to the initial payload slot.
    /// </summary>
    private static void PruneRedundantConditionalSubFields(Context context)
    {
        if (context.Conditionals is null || context.Slots is null)
        {
            return;
        }

        List<Conditional>? conditionalsToRemove = null;

        foreach (var (conditional, conditionalContext) in context.Conditionals)
        {
            var coverLimit = GetCoverLimit(context, conditional);

            if (IsCoveredBy(conditionalContext, context, coverLimit))
            {
                ClearContext(conditionalContext);
                conditionalsToRemove ??= [];
                conditionalsToRemove.Add(conditional);
            }
        }

        if (conditionalsToRemove is not null)
        {
            foreach (var c in conditionalsToRemove)
            {
                context.RemoveConditionalContext(c);
            }
        }
    }

    /// <summary>
    /// Computes the exclusive upper bound for matching cover slots when wholesale-pruning a
    /// conditional. The limit is the conditional's own slot index: covers must come from
    /// earlier (textually preceding) siblings, otherwise dropping the conditional would shift
    /// the first-occurrence position of its fields to a later slot, violating the spec's
    /// depth-first first-occurrence response ordering.
    /// </summary>
    private static int GetCoverLimit(Context context, Conditional conditional)
    {
        if (context.Slots is null)
        {
            return 0;
        }

        for (var i = 0; i < context.Slots.Count; i++)
        {
            if (context.Slots[i].Kind == SlotKind.Conditional
                && ReferenceEquals(context.Slots[i].Key, conditional))
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// Returns <c>true</c> when every direct selection in <paramref name="prune"/> is also
    /// delivered by <paramref name="keep"/> in the same relative textual order, restricted
    /// to keep-slots with index less than <paramref name="keepSlotLimit"/>. Composite
    /// selections recurse with no upper bound (the position constraint only applies at the
    /// outer level where the conditional sits).
    /// </summary>
    private static bool IsCoveredBy(Context prune, Context keep, int keepSlotLimit)
    {
        if (prune.Slots is null || prune.Slots.Count == 0)
        {
            return true;
        }

        if (keep.Slots is null)
        {
            return false;
        }

        var cap = Math.Min(keepSlotLimit, keep.Slots.Count);
        var keepIndex = 0;

        foreach (var pruneSlot in prune.Slots)
        {
            if (!SlotHasContent(prune, pruneSlot))
            {
                continue;
            }

            var matched = false;

            for (var j = keepIndex; j < cap; j++)
            {
                if (SlotIsCovered(prune, pruneSlot, keep, keep.Slots[j]))
                {
                    matched = true;
                    keepIndex = j + 1;
                    break;
                }
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SlotHasContent(Context context, SlotKey slot)
    {
        switch (slot.Kind)
        {
            case SlotKind.Field:
                return context.Fields is not null
                    && context.Fields.TryGetValue((string)slot.Key, out var fieldLookup)
                    && fieldLookup.Count > 0;

            case SlotKind.Fragment:
                var fragmentNode = (InlineFragmentNode)slot.Key;
                var typeName = fragmentNode.TypeCondition?.Name.Value ?? string.Empty;
                return context.Fragments is not null
                    && context.Fragments.TryGetValue(typeName, out var fragmentLookup)
                    && fragmentLookup.ContainsKey(fragmentNode);

            case SlotKind.Conditional:
                return context.Conditionals?.ContainsKey((Conditional)slot.Key) == true;

            default:
                return false;
        }
    }

    private static bool SlotIsCovered(Context prune, SlotKey pruneSlot, Context keep, SlotKey keepSlot)
    {
        if (pruneSlot.Kind != keepSlot.Kind)
        {
            return false;
        }

        switch (pruneSlot.Kind)
        {
            case SlotKind.Field:
                return FieldSlotIsCovered(prune, (string)pruneSlot.Key, keep, (string)keepSlot.Key);

            case SlotKind.Fragment:
                return FragmentSlotIsCovered(
                    prune,
                    (InlineFragmentNode)pruneSlot.Key,
                    keep,
                    (InlineFragmentNode)keepSlot.Key);

            case SlotKind.Conditional:
                return ReferenceEquals(pruneSlot.Key, keepSlot.Key)
                    && prune.Conditionals is not null
                    && keep.Conditionals is not null
                    && prune.Conditionals.TryGetValue((Conditional)pruneSlot.Key, out var prunedNested)
                    && keep.Conditionals.TryGetValue((Conditional)keepSlot.Key, out var keptNested)
                    && IsCoveredBy(prunedNested, keptNested, int.MaxValue);

            default:
                return false;
        }
    }

    private static bool FieldSlotIsCovered(Context prune, string pruneResponseName, Context keep, string keepResponseName)
    {
        if (!string.Equals(pruneResponseName, keepResponseName, StringComparison.Ordinal)
            || prune.Fields is null
            || !prune.Fields.TryGetValue(pruneResponseName, out var pruneFieldLookup)
            || keep.Fields is null
            || !keep.Fields.TryGetValue(keepResponseName, out var keepFieldLookup))
        {
            return false;
        }

        foreach (var (pruneFieldNode, pruneFieldContext) in pruneFieldLookup)
        {
            if (!keepFieldLookup.TryGetValue(pruneFieldNode, out var keepFieldContext))
            {
                return false;
            }

            if (pruneFieldNode.SelectionSet is null)
            {
                // Leaf: the keep side delivers the same FieldNode at the same position.
                continue;
            }

            if (pruneFieldContext is null || keepFieldContext is null)
            {
                return false;
            }

            if (!IsCoveredBy(pruneFieldContext, keepFieldContext, int.MaxValue))
            {
                return false;
            }
        }

        return true;
    }

    private static bool FragmentSlotIsCovered(
        Context prune,
        InlineFragmentNode pruneFragmentNode,
        Context keep,
        InlineFragmentNode keepFragmentNode)
    {
        if (!InlineFragmentNodeComparer.Instance.Equals(pruneFragmentNode, keepFragmentNode)
            || prune.Fragments is null
            || keep.Fragments is null)
        {
            return false;
        }

        var typeName = pruneFragmentNode.TypeCondition?.Name.Value ?? string.Empty;

        if (!prune.Fragments.TryGetValue(typeName, out var pruneFragmentLookup)
            || !keep.Fragments.TryGetValue(typeName, out var keepFragmentLookup)
            || !pruneFragmentLookup.TryGetValue(pruneFragmentNode, out var pruneFragmentContext)
            || !keepFragmentLookup.TryGetValue(keepFragmentNode, out var keepFragmentContext))
        {
            return false;
        }

        return IsCoveredBy(pruneFragmentContext, keepFragmentContext, int.MaxValue);
    }

    private static void ClearContext(Context context)
    {
        context.Fields?.Clear();
        context.Fragments?.Clear();
        context.Conditionals?.Clear();
        context.Slots?.Clear();
    }

    /// <summary>
    /// Folds a conditional sibling whose sole content matches the immediately following
    /// unconditional sibling into a sub-conditional of that unconditional. This is only safe
    /// when the two slots are textually adjacent and the conditional wraps a single matching
    /// selection: the response-name's position is then invariant under the variable and no
    /// other slot can shift across the fold. <c>@defer</c> is not eligible because it cannot
    /// be pushed onto a field selection.
    /// </summary>
    private static void FoldAdjacentSameKeyConditionals(Context context)
    {
        if (context.Slots is null || context.Slots.Count < 2 || context.Conditionals is null)
        {
            return;
        }

        for (var i = context.Slots.Count - 2; i >= 0; i--)
        {
            var condSlot = context.Slots[i];

            if (condSlot.Kind != SlotKind.Conditional)
            {
                continue;
            }

            var nextSlot = context.Slots[i + 1];

            if (nextSlot.Kind == SlotKind.Conditional)
            {
                continue;
            }

            var conditional = (Conditional)condSlot.Key;

            if (conditional.IsDeferOnly || conditional.Defer is not null)
            {
                continue;
            }

            if (!context.Conditionals.TryGetValue(conditional, out var conditionalContext))
            {
                continue;
            }

            if (conditionalContext.Slots is null || conditionalContext.Slots.Count != 1)
            {
                continue;
            }

            var innerSlot = conditionalContext.Slots[0];

            if (innerSlot.Kind != nextSlot.Kind)
            {
                continue;
            }

            if (innerSlot.Kind == SlotKind.Field
                && TryFoldFieldSlot(context, conditional, conditionalContext, (string)innerSlot.Key, (string)nextSlot.Key))
            {
                continue;
            }

            if (innerSlot.Kind == SlotKind.Fragment
                && TryFoldFragmentSlot(
                    context,
                    conditional,
                    conditionalContext,
                    (InlineFragmentNode)innerSlot.Key,
                    (InlineFragmentNode)nextSlot.Key))
            {
                continue;
            }
        }
    }

    private static bool TryFoldFieldSlot(
        Context context,
        Conditional conditional,
        Context conditionalContext,
        string innerResponseName,
        string nextResponseName)
    {
        if (!string.Equals(innerResponseName, nextResponseName, StringComparison.Ordinal)
            || conditionalContext.Fields is null
            || !conditionalContext.Fields.TryGetValue(innerResponseName, out var conditionalFieldLookup)
            || conditionalFieldLookup.Count != 1
            || context.Fields is null
            || !context.Fields.TryGetValue(nextResponseName, out var unconditionalFieldLookup))
        {
            return false;
        }

        FieldNode? conditionalFieldNode = null;
        Context? conditionalFieldContext = null;

        foreach (var (key, value) in conditionalFieldLookup)
        {
            conditionalFieldNode = key;
            conditionalFieldContext = value;
        }

        if (conditionalFieldNode is null
            || !unconditionalFieldLookup.TryGetValue(conditionalFieldNode, out var unconditionalFieldContext))
        {
            return false;
        }

        if (unconditionalFieldContext is not null && conditionalFieldContext is not null)
        {
            // Skip the fold when the unconditional already carries the same conditional key:
            // creating a second slot for the same key would leave two incoherent conditional
            // entries side by side.
            if (unconditionalFieldContext.Conditionals?.ContainsKey(conditional) == true)
            {
                return false;
            }

            var migratedConditional = unconditionalFieldContext.PrependConditionalContext(conditional);
            MergeContextsDirect(conditionalFieldContext, migratedConditional);

            // Migrating overlapping content creates a fresh adjacency at the inner level
            // (e.g. a conditional `dimension { height }` placed before an unconditional
            // `dimension { width }` inside the same parent). The natural per-level pass on
            // the unconditional field already ran before the migration, so the cascade has
            // to be re-triggered explicitly.
            FoldAdjacentSameKeyConditionals(unconditionalFieldContext);
        }

        conditionalContext.RemoveField(conditionalFieldNode);
        context.RemoveConditionalContext(conditional);
        return true;
    }

    private static bool TryFoldFragmentSlot(
        Context context,
        Conditional conditional,
        Context conditionalContext,
        InlineFragmentNode innerFragmentNode,
        InlineFragmentNode nextFragmentNode)
    {
        if (!InlineFragmentNodeComparer.Instance.Equals(innerFragmentNode, nextFragmentNode))
        {
            return false;
        }

        var typeName = innerFragmentNode.TypeCondition?.Name.Value ?? string.Empty;

        if (conditionalContext.Fragments is null
            || !conditionalContext.Fragments.TryGetValue(typeName, out var conditionalFragmentLookup)
            || conditionalFragmentLookup.Count != 1
            || context.Fragments is null
            || !context.Fragments.TryGetValue(typeName, out var unconditionalFragmentLookup))
        {
            return false;
        }

        InlineFragmentNode? conditionalFragmentNode = null;
        Context? conditionalFragmentContext = null;

        foreach (var (key, value) in conditionalFragmentLookup)
        {
            conditionalFragmentNode = key;
            conditionalFragmentContext = value;
        }

        if (conditionalFragmentNode is null
            || conditionalFragmentContext is null
            || !unconditionalFragmentLookup.TryGetValue(conditionalFragmentNode, out var unconditionalFragmentContext))
        {
            return false;
        }

        if (unconditionalFragmentContext.Conditionals?.ContainsKey(conditional) == true)
        {
            return false;
        }

        var migratedConditional = unconditionalFragmentContext.PrependConditionalContext(conditional);
        MergeContextsDirect(conditionalFragmentContext, migratedConditional);
        FoldAdjacentSameKeyConditionals(unconditionalFragmentContext);

        conditionalContext.RemoveFragment(conditionalFragmentNode);
        context.RemoveConditionalContext(conditional);
        return true;
    }

    /// <summary>
    /// Walks <paramref name="source"/> in textual slot order and copies each selection into
    /// <paramref name="target"/> via the low-level <see cref="Context.AddField(FieldNode, Context?)"/>
    /// / <see cref="Context.AddFragment(InlineFragmentNode, Context)"/> / <see cref="Context.GetOrAddConditionalContext"/>
    /// methods. Unlike <see cref="MergeContexts"/> this never re-routes a selection through the
    /// target's unconditional sibling chain: the migrated content is anchored exactly where the
    /// fold places it, preserving textual order. Sub-contexts are shared by reference because
    /// the source context is dropped immediately after the migration.
    /// </summary>
    private static void MergeContextsDirect(Context source, Context target)
    {
        if (source.Slots is null)
        {
            return;
        }

        foreach (var slot in source.Slots)
        {
            switch (slot.Kind)
            {
                case SlotKind.Field:
                    MigrateFieldSlot(source, target, (string)slot.Key);
                    break;

                case SlotKind.Fragment:
                    MigrateFragmentSlot(source, target, (InlineFragmentNode)slot.Key);
                    break;

                case SlotKind.Conditional:
                    MigrateConditionalSlot(source, target, (Conditional)slot.Key);
                    break;
            }
        }
    }

    private static void MigrateFieldSlot(Context source, Context target, string responseName)
    {
        if (source.Fields is null
            || !source.Fields.TryGetValue(responseName, out var sourceFieldLookup))
        {
            return;
        }

        foreach (var (fieldNode, fieldContext) in sourceFieldLookup)
        {
            if (target.HasField(fieldNode, out _))
            {
                continue;
            }

            target.AddField(fieldNode, fieldContext);
        }
    }

    private static void MigrateFragmentSlot(Context source, Context target, InlineFragmentNode fragmentNode)
    {
        var typeName = fragmentNode.TypeCondition?.Name.Value ?? string.Empty;

        if (source.Fragments is null
            || !source.Fragments.TryGetValue(typeName, out var sourceFragmentLookup)
            || !sourceFragmentLookup.TryGetValue(fragmentNode, out var fragmentContext))
        {
            return;
        }

        if (target.HasFragment(fragmentNode, out _))
        {
            return;
        }

        target.AddFragment(fragmentNode, fragmentContext);
    }

    private static void MigrateConditionalSlot(Context source, Context target, Conditional conditional)
    {
        if (source.Conditionals is null
            || !source.Conditionals.TryGetValue(conditional, out var sourceConditionalContext))
        {
            return;
        }

        var targetConditionalContext = target.GetOrAddConditionalContext(conditional);
        MergeContextsDirect(sourceConditionalContext, targetConditionalContext);
    }

    private void CollectField(FieldNode fieldNode, Context context)
    {
        var (conditional, directives) = DivideDirectives(
            fieldNode,
            Types.DirectiveLocation.Field);

        if (conditional is not null)
        {
            if (IsStaticallySkipped(conditional, context, out conditional))
            {
                return;
            }

            if (conditional is not null)
            {
                context = context.GetOrAddConditionalContext(conditional);
            }
        }

        fieldNode = new FieldNode(
            fieldNode.Name,
            fieldNode.Alias,
            directives ?? [],
            RewriteArguments(fieldNode.Arguments),
            fieldNode.SelectionSet);

        var fieldName = fieldNode.Name.Value;
        ITypeDefinition? fieldType = null;

        if (fieldNode.SelectionSet is not null && context.Type is IComplexTypeDefinition complexType)
        {
            var field = complexType.Fields[fieldName];

            fieldType = field.Type.AsTypeDefinition();
        }

        var fieldContext = GetOrAddContextForField(context, fieldNode, fieldType);

        if (fieldContext is not null && fieldNode.SelectionSet is not null)
        {
            CollectSelections(fieldNode.SelectionSet, fieldContext);
        }
    }

    private void CollectInlineFragment(InlineFragmentNode inlineFragment, Context context)
    {
        var typeCondition = inlineFragment.TypeCondition is not null
            ? schema.Types[inlineFragment.TypeCondition.Name.Value]
            : context.Type;

        var (conditional, directives) = DivideDirectives(
            inlineFragment,
            Types.DirectiveLocation.InlineFragment);

        CollectFragment(
            inlineFragment.SelectionSet,
            typeCondition,
            conditional,
            directives,
            context);
    }

    private void CollectFragmentSpread(FragmentSpreadNode fragmentSpread, Context context)
    {
        var fragmentDefinition = context.GetFragmentDefinition(fragmentSpread.Name.Value);
        var typeCondition = schema.Types[fragmentDefinition.TypeCondition.Name.Value];

        var (conditional, directives) = DivideDirectives(
            fragmentSpread,
            Types.DirectiveLocation.InlineFragment);

        CollectFragment(
            fragmentDefinition.SelectionSet,
            typeCondition,
            conditional,
            directives,
            context);
    }

    private void CollectFragment(
        SelectionSetNode selectionSet,
        ITypeDefinition typeCondition,
        Conditional? conditional,
        IReadOnlyList<DirectiveNode>? otherDirectives,
        Context context)
    {
        var inlinesIntoCallerContext = conditional is null;

        if (conditional is not null)
        {
            if (IsStaticallySkipped(conditional, context, out conditional))
            {
                return;
            }

            if (conditional is not null)
            {
                context = context.GetOrAddConditionalContext(conditional);
            }
            else
            {
                inlinesIntoCallerContext = true;
            }
        }

        var isTypeRefinement = !typeCondition.IsAssignableFrom(context.Type);

        if (isTypeRefinement || otherDirectives is not null)
        {
            var inlineFragment = new InlineFragmentNode(
                null,
                isTypeRefinement
                    ? new NamedTypeNode(typeCondition.Name)
                    : null,
                otherDirectives ?? [],
                selectionSet);

            var fragmentContext = GetOrAddContextForFragment(context, inlineFragment, typeCondition);
            CollectSelections(selectionSet, fragmentContext);
        }
        else if (inlinesIntoCallerContext)
        {
            // The fragment inlines into the same context that is still being collected.
            // Skip the post-passes here, the outer CollectSelections will run them once after
            // all sibling selections have been added.
            CollectSelectionsCore(selectionSet, context);
        }
        else
        {
            // A conditional fragment with no type refinement and no other directives:
            // the fragment's selections land in a fresh conditional sub-context that has its
            // own post-pass requirements.
            CollectSelections(selectionSet, context);
        }
    }

    private static Context? GetOrAddContextForField(Context context, FieldNode fieldNode, ITypeDefinition? fieldType)
    {
        if (context.IsConditionalContext)
        {
            var unconditionalContext = context.UnconditionalContext;

            // The unconditional sibling was collected before this conditional occurrence
            // (otherwise we would be in the other branch). The unconditional slot already
            // owns the response position, so the conditional occurrence is subsumed and we
            // hand back the unconditional field context for any deeper merging.
            if (unconditionalContext.HasField(fieldNode, out var unconditionalFieldContext))
            {
                if (fieldNode.SelectionSet is null)
                {
                    return null;
                }

                if (unconditionalFieldContext is null)
                {
                    throw new InvalidOperationException("Expected to have a field context");
                }

                var conditionalContextBelowUnconditionalFieldContext =
                    RecreateConditionalContextHierarchy(unconditionalFieldContext, context);

                return conditionalContextBelowUnconditionalFieldContext;
            }

            if (!context.HasField(fieldNode, out var fieldContext))
            {
                fieldContext = context.AddField(fieldNode, fieldType);
            }

            return fieldContext;
        }
        else
        {
            // We are adding the unconditional occurrence now. If a conditional sibling added
            // the same response name earlier (i.e. textually before this point), keep both:
            // the runtime response order depends on the variable, so the slots cannot be
            // folded into a single position. The conditional occurrence stays at its slot,
            // the unconditional occurrence takes a new slot at the current position.
            if (!context.HasField(fieldNode, out var fieldContext))
            {
                fieldContext = context.AddField(fieldNode, fieldType);
            }

            return fieldContext;
        }
    }

    private static Context GetOrAddContextForFragment(
        Context context,
        InlineFragmentNode inlineFragmentNode,
        ITypeDefinition typeCondition)
    {
        if (context.IsConditionalContext)
        {
            var unconditionalContext = context.UnconditionalContext;

            // The unconditional sibling was collected before this conditional occurrence,
            // so its slot owns the position. Recurse into the unconditional context for
            // any deeper merging.
            if (unconditionalContext.HasFragment(inlineFragmentNode, out var unconditionalFragmentContext))
            {
                var conditionalContextBelowUnconditionalFragmentContext =
                    RecreateConditionalContextHierarchy(unconditionalFragmentContext, context);

                return conditionalContextBelowUnconditionalFragmentContext;
            }

            if (!context.HasFragment(inlineFragmentNode, out var fragmentContext))
            {
                fragmentContext = context.AddFragment(inlineFragmentNode, typeCondition);
            }

            return fragmentContext;
        }
        else
        {
            // Adding the unconditional occurrence. If a conditional sibling added the same
            // fragment earlier, keep both: runtime ordering depends on the variable and
            // the slots cannot be folded.
            if (!context.HasFragment(inlineFragmentNode, out var fragmentContext))
            {
                fragmentContext = context.AddFragment(inlineFragmentNode, typeCondition);
            }

            return fragmentContext;
        }
    }

    /// <summary>
    /// Rebuilds the conditional directive hierarchy of <paramref name="sourceContext"/> into
    /// <paramref name="targetContext"/>, returning the innermost, rebuilt conditional context.
    /// </summary>
    private static Context RecreateConditionalContextHierarchy(Context targetContext, Context sourceContext)
    {
        var conditionalStack = new Stack<Context>();
        var current = sourceContext;

        while (current?.IsConditionalContext == true)
        {
            conditionalStack.Push(current);
            current = current.Parent;
        }

        while (conditionalStack.TryPop(out var conditionalContext))
        {
            targetContext = targetContext.GetOrAddConditionalContext(conditionalContext.Conditional!);
        }

        return targetContext;
    }

    private static void MergeContexts(Context source, Context target)
    {
        if (source.Conditionals is not null)
        {
            foreach (var (conditional, conditionalContext) in source.Conditionals)
            {
                var targetConditionalContext = target.GetOrAddConditionalContext(conditional);

                MergeContexts(conditionalContext, targetConditionalContext);
            }
        }

        if (source.Fields is not null)
        {
            foreach (var (_, fieldContextLookup) in source.Fields)
            {
                foreach (var (fieldNode, fieldContext) in fieldContextLookup)
                {
                    if (!target.HasField(fieldNode, out var targetFieldContext))
                    {
                        targetFieldContext = GetOrAddContextForField(target, fieldNode, fieldContext?.Type);
                    }

                    if (fieldContext is not null && targetFieldContext is not null)
                    {
                        MergeContexts(fieldContext, targetFieldContext);
                    }
                }
            }
        }

        if (source.Fragments is not null)
        {
            foreach (var (_, fragmentContextLookup) in source.Fragments)
            {
                foreach (var (inlineFragmentNode, fragmentContext) in fragmentContextLookup)
                {
                    if (!target.HasFragment(inlineFragmentNode, out var targetFragmentContext))
                    {
                        targetFragmentContext = GetOrAddContextForFragment(
                            target,
                            inlineFragmentNode,
                            fragmentContext.Type);
                    }

                    MergeContexts(fragmentContext, targetFragmentContext);
                }
            }
        }
    }

    private (Conditional? Conditional, IReadOnlyList<DirectiveNode>? Directives) DivideDirectives(
        IHasDirectives directiveProvider,
        Types.DirectiveLocation targetLocation)
    {
        if (directiveProvider.Directives.Count == 0)
        {
            return (null, null);
        }

        Conditional? conditional = null;
        List<DirectiveNode>? directives = null;

        foreach (var directive in directiveProvider.Directives)
        {
            if (schema.DirectiveDefinitions.TryGetDirective(directive.Name.Value, out var directiveDefinition)
                && !directiveDefinition.Locations.HasFlag(targetLocation))
            {
                continue;
            }

            var rewrittenDirective = RewriteDirective(directive);

            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal))
            {
                if (directive.Arguments is [{ Value: BooleanValueNode }])
                {
                    continue;
                }

                conditional ??= new Conditional();
                conditional.Skip = rewrittenDirective;

                continue;
            }

            if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal))
            {
                if (directive.Arguments is [{ Value: BooleanValueNode }])
                {
                    continue;
                }

                conditional ??= new Conditional();
                conditional.Include = rewrittenDirective;

                continue;
            }

            if (directive.Name.Value.Equals(DirectiveNames.Defer.Name, StringComparison.Ordinal))
            {
                var ifArgument = directive.Arguments
                    .FirstOrDefault(a => a.Name.Value.Equals(
                        DirectiveNames.Defer.Arguments.If,
                        StringComparison.Ordinal));

                if (ifArgument?.Value is BooleanValueNode { Value: false })
                {
                    continue;
                }

                if (targetLocation != Types.DirectiveLocation.Field)
                {
                    conditional ??= new Conditional();
                    conditional.Defer = NormalizeDeferDirective(rewrittenDirective);
                    _hasIncrementalParts = true;

                    continue;
                }
            }

            if (directive.Name.Value.Equals(DirectiveNames.Stream.Name, StringComparison.Ordinal)
                && targetLocation == Types.DirectiveLocation.Field)
            {
                _hasIncrementalParts = true;
            }

            directives ??= [];
            directives.Add(rewrittenDirective);
        }

        return (conditional, directives);
    }

    /// <summary>
    /// Checks whether a parent context already contains an opposite conditional part
    /// with the same variable value, e.g. <c>@skip(if: $value) :: @include(if: $value)</c>.
    /// <br/>
    /// If that's the case, the child conditional will always be skipped, so we can statically
    /// skip the selection this conditional is on.
    /// <br/>
    /// Additionally, we check whether a part of the conditional already exists in a parent context.
    /// <br/>
    /// In a hierarchy like <c>@skip(if: $skip) -> something -> @skip(if: $skip)</c>,
    /// we can get rid of the second @skip, since it will always be included.
    /// </summary>
    private static bool IsStaticallySkipped(
        Conditional conditional,
        Context context,
        out Conditional? newConditional)
    {
        newConditional = conditional;

        var current = context;
        do
        {
            if (current.Conditional is { } parentConditional)
            {
                if (parentConditional.Skip is not null)
                {
                    if (newConditional.Skip?.Equals(parentConditional.Skip, SyntaxComparison.Syntax) == true)
                    {
                        // If the parent has exactly the same @skip, we can remove the new one.
                        newConditional.Skip = null;
                    }
                    else if (ConditionalDirectiveHasSameVariable(parentConditional.Skip, newConditional.Include))
                    {
                        // If the parent has a @skip with the same variable as the new @include,
                        // the new @include will never be included, so the selection its on
                        // can be statically removed.
                        return true;
                    }
                }

                if (parentConditional.Include is not null)
                {
                    if (newConditional.Include?.Equals(parentConditional.Include, SyntaxComparison.Syntax) == true)
                    {
                        // If the parent has exactly the same @include, we can remove the new one.
                        newConditional.Include = null;
                    }
                    else if (ConditionalDirectiveHasSameVariable(parentConditional.Include, newConditional.Skip))
                    {
                        // If the parent has a @include with the same variable as the new @skip,
                        // the new @skip will never be included, so the selection its on
                        // can be statically removed.
                        return true;
                    }
                }

                if (parentConditional.Defer is not null
                    && newConditional.Defer?.Equals(parentConditional.Defer, SyntaxComparison.Syntax) == true)
                {
                    // If the parent has exactly the same @defer, we can remove the new one.
                    newConditional.Defer = null;
                }

                if (newConditional.Skip is null
                    && newConditional.Include is null
                    && newConditional.Defer is null)
                {
                    // All of the @skip, @include and @defer in the new conditional have already
                    // appeared on a parent, so we can get rid of the entire conditional.
                    newConditional = null;
                    break;
                }
            }

            current = current.Parent;
        } while (current is not null);

        return false;
    }

    private static bool ConditionalDirectiveHasSameVariable(DirectiveNode directive1, DirectiveNode? directive2)
    {
        if (directive2 is null)
        {
            return false;
        }

        var if1 = directive1.Arguments[0];
        var if2 = directive2.Arguments[0];

        return if1.Value.Equals(if2.Value, SyntaxComparison.Syntax);
    }

    private static bool IsStaticallySkipped(IHasDirectives directiveProvider)
    {
        if (directiveProvider.Directives.Count == 0)
        {
            return false;
        }

        foreach (var directive in directiveProvider.Directives)
        {
            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal)
                && directive.Arguments is [{ Value: BooleanValueNode { Value: true } }])
            {
                return true;
            }

            if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal)
                && directive.Arguments is [{ Value: BooleanValueNode { Value: false } }])
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, FragmentDefinitionNode> CreateFragmentLookup(DocumentNode document)
    {
        var lookup = new Dictionary<string, FragmentDefinitionNode>();

        foreach (var definition in document.Definitions)
        {
            if (definition is FragmentDefinitionNode fragmentDef)
            {
                lookup.Add(fragmentDef.Name.Value, fragmentDef);
            }
        }

        return lookup;
    }

    #endregion

    #region Rewriting

    private List<ISelectionNode>? RewriteSelections(Context context)
    {
        List<ISelectionNode>? selections = null;

        if (context.Slots is null)
        {
            return selections;
        }

        foreach (var slot in context.Slots)
        {
            switch (slot.Kind)
            {
                case SlotKind.Field:
                    AppendFieldSlot(context, (string)slot.Key, ref selections);
                    break;

                case SlotKind.Fragment:
                    AppendFragmentSlot(context, (InlineFragmentNode)slot.Key, ref selections);
                    break;

                case SlotKind.Conditional:
                    AppendConditionalSlot(context, (Conditional)slot.Key, ref selections);
                    break;
            }
        }

        return selections;
    }

    private void AppendFieldSlot(
        Context context,
        string responseName,
        ref List<ISelectionNode>? selections)
    {
        if (context.Fields is null
            || !context.Fields.TryGetValue(responseName, out var fieldContextLookup))
        {
            return;
        }

        foreach (var (fieldNode, fieldContext) in fieldContextLookup)
        {
            var newFieldNode = RewriteField(fieldNode, fieldContext);

            if (newFieldNode is null)
            {
                continue;
            }

            selections ??= [];
            selections.Add(newFieldNode);
        }
    }

    private void AppendFragmentSlot(
        Context context,
        InlineFragmentNode inlineFragmentNode,
        ref List<ISelectionNode>? selections)
    {
        var typeName = inlineFragmentNode.TypeCondition?.Name.Value ?? string.Empty;

        if (context.Fragments is null
            || !context.Fragments.TryGetValue(typeName, out var fragmentContextLookup)
            || !fragmentContextLookup.TryGetValue(inlineFragmentNode, out var fragmentContext))
        {
            return;
        }

        var newInlineFragmentNode = RewriteInlineFragment(inlineFragmentNode, fragmentContext);

        if (newInlineFragmentNode is null)
        {
            return;
        }

        selections ??= [];
        selections.Add(newInlineFragmentNode);
    }

    private void AppendConditionalSlot(
        Context context,
        Conditional conditional,
        ref List<ISelectionNode>? selections)
    {
        if (context.Conditionals is null
            || !context.Conditionals.TryGetValue(conditional, out var conditionalContext))
        {
            return;
        }

        var conditionalSelection = RewriteConditional(conditional, conditionalContext);

        if (conditionalSelection is null)
        {
            return;
        }

        selections ??= [];
        selections.Add(conditionalSelection);
    }

    private ISelectionNode? RewriteConditional(
        Conditional conditional,
        Context context)
    {
        var conditionalSelections = RewriteSelections(context);

        if (conditionalSelections is null)
        {
            return null;
        }

        var conditionalDirectives = conditional.ToDirectives();

        // If we only have a single selection and this selection does not have directives of its own,
        // we can push the conditional directives down on it.
        // Otherwise we return an inline fragment with all the conditional selections.
        // @defer is only valid on fragments, so a single field cannot absorb a conditional
        // that contains @defer.
        return conditionalSelections switch
        {
            [FieldNode { Directives.Count: 0 } fieldNode] when conditional.Defer is null => fieldNode
                .WithDirectives([.. fieldNode.Directives, .. conditionalDirectives]),
            [InlineFragmentNode { Directives.Count: 0 } inlineFragmentNode] => inlineFragmentNode
                .WithDirectives([.. inlineFragmentNode.Directives, .. conditionalDirectives]),
            _ => new InlineFragmentNode(
                null,
                null,
                conditionalDirectives.ToArray(),
                new SelectionSetNode(conditionalSelections))
        };
    }

    private FieldNode? RewriteField(
        FieldNode fieldNode,
        Context? fieldContext)
    {
        if (fieldNode.SelectionSet is null)
        {
            return fieldNode;
        }

        if (fieldContext is null)
        {
            throw new InvalidOperationException("Expected to have field context");
        }

        var fieldSelections = RewriteSelections(fieldContext);

        if (fieldSelections is null)
        {
            if (!removeStaticallyExcludedSelections)
            {
                return null;
            }

            fieldSelections = includeTypeNameToEmptySelectionSets ? [s_typeNameField] : [];
        }

        return fieldNode.WithSelectionSet(new SelectionSetNode(fieldSelections));
    }

    private InlineFragmentNode? RewriteInlineFragment(
        InlineFragmentNode inlineFragmentNode,
        Context fragmentContext)
    {
        var fragmentSelections = RewriteSelections(fragmentContext);

        if (fragmentSelections is null)
        {
            if (!removeStaticallyExcludedSelections)
            {
                return null;
            }

            fragmentSelections = includeTypeNameToEmptySelectionSets ? [s_typeNameField] : [];
        }

        return inlineFragmentNode.WithSelectionSet(new SelectionSetNode(fragmentSelections));
    }

    private static IReadOnlyList<DirectiveNode> RewriteDirectives(IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return directives;
        }

        if (directives.Count == 1)
        {
            return ImmutableArray<DirectiveNode>.Empty.Add(RewriteDirective(directives[0]));
        }

        var buffer = new DirectiveNode[directives.Count];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = RewriteDirective(directives[i]);
        }

        return ImmutableArray.Create(buffer);
    }

    private static DirectiveNode RewriteDirective(DirectiveNode directive)
    {
        return new DirectiveNode(directive.Name.Value, RewriteArguments(directive.Arguments));
    }

    /// <summary>
    /// Drops a literal <c>if: true</c> argument from a @defer directive so that
    /// <c>@defer</c> and <c>@defer(if: true)</c> share the same identity and can be merged.
    /// </summary>
    private static DirectiveNode NormalizeDeferDirective(DirectiveNode deferDirective)
    {
        if (deferDirective.Arguments.Count == 0)
        {
            return deferDirective;
        }

        ArgumentNode? ifArgument = null;
        for (var i = 0; i < deferDirective.Arguments.Count; i++)
        {
            var arg = deferDirective.Arguments[i];

            if (arg.Name.Value.Equals(DirectiveNames.Defer.Arguments.If, StringComparison.Ordinal))
            {
                ifArgument = arg;
                break;
            }
        }

        if (ifArgument?.Value is not BooleanValueNode { Value: true })
        {
            return deferDirective;
        }

        if (deferDirective.Arguments.Count == 1)
        {
            return new DirectiveNode(DirectiveNames.Defer.Name, ImmutableArray<ArgumentNode>.Empty);
        }

        var remainingArguments = new ArgumentNode[deferDirective.Arguments.Count - 1];
        var index = 0;

        foreach (var arg in deferDirective.Arguments)
        {
            if (!arg.Name.Value.Equals(DirectiveNames.Defer.Arguments.If, StringComparison.Ordinal))
            {
                remainingArguments[index++] = arg;
            }
        }

        return new DirectiveNode(DirectiveNames.Defer.Name, ImmutableArray.Create(remainingArguments));
    }

    private static IReadOnlyList<ArgumentNode> RewriteArguments(IReadOnlyList<ArgumentNode> arguments)
        => arguments;

    #endregion

    [DebuggerDisplay(
        "{Type.Name}, Fields: {Fields?.Count}, Fragments: {Fragments?.Count}, Conditionals: {Conditionals?.Count}")]
    private sealed class Context(
        Context? parent,
        Context? unconditionalContext,
        ITypeDefinition type,
        Conditional? conditional,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
    {
        /// <summary>
        /// Points to the parent of this context.
        /// Null for the root context.
        /// </summary>
        public Context? Parent { get; set; } = parent;

        /// <summary>
        /// The type selections in this context are on.
        /// </summary>
        public ITypeDefinition Type { get; } = type;

        [MemberNotNullWhen(true, nameof(Conditional))]
        [MemberNotNullWhen(true, nameof(UnconditionalContext))]
        public bool IsConditionalContext { get; } = conditional is not null;

        /// <summary>
        /// Contains the conditional, if this context is conditional.
        /// </summary>
        public Conditional? Conditional { get; } = conditional;

        /// <summary>
        /// If this context is conditional, this points to the unconditional version
        /// of the context.
        /// </summary>
        public Context? UnconditionalContext { get; } = unconditionalContext;

        /// <summary>
        /// The context for a specific <see cref="DocumentRewriter.Conditional"/>.
        /// </summary>
        public Dictionary<Conditional, Context>? Conditionals { get; private set; }

        /// <summary>
        /// Provides a fast way to get all FieldNodes for the same response name.
        /// The key is the response name.
        /// </summary>
        public Dictionary<string, Dictionary<FieldNode, Context?>>? Fields { get; private set; }

        /// <summary>
        /// Provides a fast way to get all InlineFragmentNodes of the same type refinement.
        /// The key is the name of the type being refined to or an empty string
        /// for an inline fragment without type refinement.
        /// </summary>
        public Dictionary<string, Dictionary<InlineFragmentNode, Context>>? Fragments { get; private set; }

        /// <summary>
        /// Records the textual order in which selections were collected so that emission
        /// reproduces the spec-mandated depth-first first-occurrence ordering across
        /// fields, fragments and conditional groups.
        /// </summary>
        public List<SlotKey>? Slots { get; private set; }

        public FragmentDefinitionNode GetFragmentDefinition(string name)
            => fragmentLookup[name];

        public Context GetOrAddConditionalContext(Conditional conditional)
        {
            Conditionals ??= [];

            if (!Conditionals.TryGetValue(conditional, out var conditionalContext))
            {
                conditionalContext = new Context(
                    this,
                    GetUnconditionalContext(),
                    Type,
                    conditional,
                    fragmentLookup);

                Conditionals[conditional] = conditionalContext;

                Slots ??= [];
                Slots.Add(new SlotKey(SlotKind.Conditional, conditional));
            }

            return conditionalContext;
        }

        /// <summary>
        /// Creates a new conditional context for the given <paramref name="conditional"/> and
        /// places its slot at index 0 so its content emits before any existing selections at
        /// this level. The caller is responsible for ensuring no entry with the same key
        /// exists yet.
        /// </summary>
        public Context PrependConditionalContext(Conditional conditional)
        {
            Conditionals ??= [];
            Slots ??= [];

            var conditionalContext = new Context(
                this,
                GetUnconditionalContext(),
                Type,
                conditional,
                fragmentLookup);

            Conditionals[conditional] = conditionalContext;
            Slots.Insert(0, new SlotKey(SlotKind.Conditional, conditional));

            return conditionalContext;
        }

        public bool HasField(FieldNode fieldNode, out Context? fieldContext)
        {
            fieldContext = null;

            if (Fields is null)
            {
                return false;
            }

            var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;

            if (!Fields.TryGetValue(responseName, out var existingFieldContextLookup))
            {
                return false;
            }

            return existingFieldContextLookup.TryGetValue(fieldNode, out fieldContext);
        }

        public Context? AddField(FieldNode fieldNode, ITypeDefinition? fieldType)
        {
            Context? fieldContext = null;

            if (fieldNode.SelectionSet is not null && fieldType is not null)
            {
                fieldContext = new Context(
                    this,
                    GetUnconditionalContext(),
                    fieldType,
                    null,
                    fragmentLookup);
            }

            AddField(fieldNode, fieldContext);

            return fieldContext;
        }

        public void AddField(FieldNode fieldNode, Context? fieldContext)
        {
            Fields ??= [];

            var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;

            if (!Fields.TryGetValue(responseName, out var existingFieldContextLookup))
            {
                existingFieldContextLookup = new(FieldNodeComparer.Instance);
                Fields[responseName] = existingFieldContextLookup;

                Slots ??= [];
                Slots.Add(new SlotKey(SlotKind.Field, responseName));
            }

            existingFieldContextLookup.Add(fieldNode, fieldContext);
        }

        public void RemoveField(FieldNode fieldNode)
        {
            if (Fields is null)
            {
                return;
            }

            var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;

            if (!Fields.TryGetValue(responseName, out var existingFieldContextLookup))
            {
                return;
            }

            existingFieldContextLookup.Remove(fieldNode);
        }

        public bool HasFragment(
            InlineFragmentNode inlineFragmentNode,
            [NotNullWhen(true)] out Context? fragmentContext)
        {
            fragmentContext = null;

            if (Fragments is null)
            {
                return false;
            }

            var typeName = inlineFragmentNode.TypeCondition?.Name.Value ?? string.Empty;

            if (!Fragments.TryGetValue(typeName, out var existingFragmentContextLookup))
            {
                return false;
            }

            return existingFragmentContextLookup.TryGetValue(inlineFragmentNode, out fragmentContext);
        }

        public Context AddFragment(InlineFragmentNode inlineFragmentNode, ITypeDefinition typeCondition)
        {
            var fragmentContext = new Context(
                this,
                GetUnconditionalContext(),
                typeCondition,
                null,
                fragmentLookup);

            AddFragment(inlineFragmentNode, fragmentContext);

            return fragmentContext;
        }

        public void AddFragment(InlineFragmentNode inlineFragmentNode, Context fragmentContext)
        {
            Fragments ??= [];

            var typeName = inlineFragmentNode.TypeCondition?.Name.Value ?? string.Empty;

            if (!Fragments.TryGetValue(typeName, out var existingFragmentContextLookup))
            {
                existingFragmentContextLookup = new(InlineFragmentNodeComparer.Instance);
                Fragments[typeName] = existingFragmentContextLookup;
            }

            existingFragmentContextLookup.Add(inlineFragmentNode, fragmentContext);

            Slots ??= [];
            Slots.Add(new SlotKey(SlotKind.Fragment, inlineFragmentNode));
        }

        public void RemoveFragment(InlineFragmentNode inlineFragmentNode)
        {
            if (Fragments is null)
            {
                return;
            }

            var typeName = inlineFragmentNode.TypeCondition?.Name.Value ?? string.Empty;

            if (!Fragments.TryGetValue(typeName, out var existingFragmentContextLookup))
            {
                return;
            }

            existingFragmentContextLookup.Remove(inlineFragmentNode);
        }

        /// <summary>
        /// Removes a conditional context and its slot at this level.
        /// </summary>
        public void RemoveConditionalContext(Conditional conditional)
        {
            Conditionals?.Remove(conditional);

            if (Slots is null)
            {
                return;
            }

            for (var i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].Kind == SlotKind.Conditional
                    && ReferenceEquals(Slots[i].Key, conditional))
                {
                    Slots.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Returns the slot index of a selection inside this context, or -1 when the
        /// selection has no slot at this level.
        /// </summary>
        public int IndexOfSlot(SlotKind kind, object key)
        {
            if (Slots is null)
            {
                return -1;
            }

            for (var i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];

                if (slot.Kind != kind)
                {
                    continue;
                }

                if (kind == SlotKind.Field)
                {
                    if (string.Equals((string)slot.Key, (string)key, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }
                else if (kind == SlotKind.Fragment)
                {
                    if (InlineFragmentNodeComparer.Instance.Equals((InlineFragmentNode)slot.Key, (InlineFragmentNode)key))
                    {
                        return i;
                    }
                }
                else if (ReferenceEquals(slot.Key, key))
                {
                    return i;
                }
            }

            return -1;
        }

        private Context GetUnconditionalContext()
        {
            if (IsConditionalContext)
            {
                return UnconditionalContext;
            }

            return this;
        }
    }

    /// <summary>
    /// Holds a combination of @skip, @include and @defer.
    /// </summary>
    private sealed class Conditional
    {
        private static readonly IEqualityComparer<ISyntaxNode> s_comparer = SyntaxComparer.BySyntax;

        public DirectiveNode? Skip { get; set; }

        public DirectiveNode? Include { get; set; }

        public DirectiveNode? Defer { get; set; }

        public bool IsDeferOnly => Skip is null && Include is null && Defer is not null;

        public IEnumerable<DirectiveNode> ToDirectives()
        {
            if (Skip is not null)
            {
                yield return Skip;
            }

            if (Include is not null)
            {
                yield return Include;
            }

            if (Defer is not null)
            {
                yield return Defer;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Conditional other)
            {
                return false;
            }

            return s_comparer.Equals(Skip, other.Skip)
                && s_comparer.Equals(Include, other.Include)
                && s_comparer.Equals(Defer, other.Defer);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                GetDirectiveHashCode(Skip),
                GetDirectiveHashCode(Include),
                GetDirectiveHashCode(Defer));
        }

        public override string ToString()
        {
            var skipDirective = Skip?.ToString();
            var includeDirective = Include?.ToString();
            var deferDirective = Defer?.ToString();

            var parts = new List<string>(3);

            if (skipDirective is not null)
            {
                parts.Add(skipDirective);
            }

            if (includeDirective is not null)
            {
                parts.Add(includeDirective);
            }

            if (deferDirective is not null)
            {
                parts.Add(deferDirective);
            }

            if (parts.Count == 0)
            {
                throw new InvalidOperationException();
            }

            return string.Join(" ", parts);
        }

        private static int GetDirectiveHashCode(DirectiveNode? node)
        {
            return node is null ? 0 : s_comparer.GetHashCode(node);
        }
    }

    private enum SlotKind : byte
    {
        Field,
        Fragment,
        Conditional
    }

    /// <summary>
    /// Identifies the textual position of a selection entry inside a <see cref="Context"/>.
    /// The <see cref="Key"/> is a response name for <see cref="SlotKind.Field"/>, an
    /// <see cref="InlineFragmentNode"/> for <see cref="SlotKind.Fragment"/>, or a
    /// <see cref="Conditional"/> for <see cref="SlotKind.Conditional"/>.
    /// </summary>
    private readonly struct SlotKey(SlotKind kind, object key)
    {
        public SlotKind Kind { get; } = kind;

        public object Key { get; } = key;
    }

    #region Comparers

    private sealed class InlineFragmentNodeComparer : IEqualityComparer<InlineFragmentNode>
    {
        public bool Equals(InlineFragmentNode? x, InlineFragmentNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            return SyntaxComparer.BySyntax.Equals(x.TypeCondition, y.TypeCondition)
                && Equals(x.Directives, y.Directives);
        }

        private static bool Equals(IReadOnlyList<ISyntaxNode> a, IReadOnlyList<ISyntaxNode> b)
        {
            if (a.Count == 0 && b.Count == 0)
            {
                return true;
            }

            return a.SequenceEqual(b, SyntaxComparer.BySyntax);
        }

        public int GetHashCode(InlineFragmentNode obj)
        {
            var hashCode = new HashCode();

            if (obj.TypeCondition is not null)
            {
                hashCode.Add(obj.TypeCondition.Name.Value);
            }

            for (var i = 0; i < obj.Directives.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Directives[i]));
            }

            return hashCode.ToHashCode();
        }

        public static InlineFragmentNodeComparer Instance { get; } = new();
    }

    private sealed class FieldNodeComparer : IEqualityComparer<FieldNode>
    {
        public bool Equals(FieldNode? x, FieldNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            return Equals(x.Alias, y.Alias)
                && x.Name.Equals(y.Name)
                && Equals(x.Directives, y.Directives)
                && Equals(x.Arguments, y.Arguments);
        }

        private static bool Equals(IReadOnlyList<ISyntaxNode> a, IReadOnlyList<ISyntaxNode> b)
        {
            if (a.Count == 0 && b.Count == 0)
            {
                return true;
            }

            return a.SequenceEqual(b, SyntaxComparer.BySyntax);
        }

        public int GetHashCode(FieldNode obj)
        {
            var hashCode = new HashCode();

            if (obj.Alias is not null)
            {
                hashCode.Add(obj.Alias.Value);
            }

            hashCode.Add(obj.Name.Value);

            for (var i = 0; i < obj.Directives.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Directives[i]));
            }

            for (var i = 0; i < obj.Arguments.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Arguments[i]));
            }

            return hashCode.ToHashCode();
        }

        public static FieldNodeComparer Instance { get; } = new();
    }

    #endregion
}
