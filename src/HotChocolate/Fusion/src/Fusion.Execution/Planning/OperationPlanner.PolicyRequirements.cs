using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    private const string RequirementDirectiveName = "fusion__requirement";

    private static Dictionary<string, SelectionSetNode> CreatePolicyRequirementMap(
        IEnumerable<IPolicy> policies)
    {
        var requirements = new Dictionary<string, SelectionSetNode>(StringComparer.Ordinal);

        foreach (var policy in policies)
        {
            if (policy.Requirements.Resource is not { } selectionSet)
            {
                continue;
            }

            requirements.Add(policy.Name, selectionSet);
        }

        return requirements;
    }

    /// <summary>
    /// Builds a lookup from policy name to <see cref="PolicyRequirements.IsRequestCacheable"/>,
    /// used to classify a policy application's names into the plan-time condition slot path
    /// (all names request-cacheable) or the policy execution node path (any name reads
    /// resource or action data).
    /// </summary>
    private static Dictionary<string, bool> CreatePolicyRequestCacheabilityMap(
        IEnumerable<IPolicy> policies)
    {
        var cacheability = new Dictionary<string, bool>(StringComparer.Ordinal);

        foreach (var policy in policies)
        {
            cacheability[policy.Name] = policy.Requirements.IsRequestCacheable;
        }

        return cacheability;
    }

    /// <summary>
    /// Gets whether the named policy is known and produces a request-constant decision. An
    /// unknown policy name is treated as not request-cacheable, so classification conservatively
    /// falls back to the policy execution node path instead of gating on an unresolved policy.
    /// </summary>
    private static bool IsPolicyRequestCacheable(string policyName, PolicyPlanningState policyState)
        => policyState.RequestCacheability.TryGetValue(policyName, out var cacheable) && cacheable;

    /// <summary>
    /// Gets whether every policy name reached by <paramref name="application"/>, across every
    /// OR group, is request-cacheable. Such an application produces a request-constant decision
    /// and can be represented entirely as a plan-time condition slot.
    /// </summary>
    private static bool IsApplicationRequestCacheable(
        PolicyApplication application,
        PolicyPlanningState policyState)
    {
        foreach (var group in application.Groups)
        {
            foreach (var name in group)
            {
                if (!IsPolicyRequestCacheable(name, policyState))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Classifies a policy application at a coordinate for plan-time condition slot gating
    /// (repo-xx5): S when every name is request-cacheable, M when the application mixes
    /// request-cacheable and data-bearing names, and Npure when no name is request-cacheable.
    /// </summary>
    private static PolicyApplicationClass ClassifyApplication(
        PolicyApplication application,
        PolicyPlanningState policyState)
    {
        if (IsApplicationRequestCacheable(application, policyState))
        {
            return PolicyApplicationClass.S;
        }

        foreach (var group in application.Groups)
        {
            foreach (var name in group)
            {
                if (IsPolicyRequestCacheable(name, policyState))
                {
                    return PolicyApplicationClass.M;
                }
            }
        }

        return PolicyApplicationClass.Npure;
    }

    /// <summary>
    /// The classification of a policy application at one coordinate, used to split the
    /// coordinate into its plan-time slot-eligible applications (S union M) and its residual
    /// applications (M union Npure, the set the static residual denial threshold is computed
    /// over) per repo-xx5.
    /// </summary>
    private enum PolicyApplicationClass
    {
        /// <summary>Every policy name in the application is request-cacheable.</summary>
        S,

        /// <summary>The application mixes request-cacheable and data-bearing names.</summary>
        M,

        /// <summary>No policy name in the application is request-cacheable.</summary>
        Npure
    }

    private OperationDefinitionNode InjectPolicyRequirements(
        OperationDefinitionNode operation,
        PolicyPlanningState policyState)
    {
        var rootType = _schema.GetOperationType(operation.Operation);
        var selectionSet = RewriteSelectionSetWithPolicyRequirements(
            operation.SelectionSet,
            rootType,
            policyState);

        return ReferenceEquals(selectionSet, operation.SelectionSet)
            ? operation
            : operation.WithSelectionSet(selectionSet);
    }

    private SelectionSetNode RewriteSelectionSetWithPolicyRequirements(
        SelectionSetNode selectionSet,
        ITypeDefinition type,
        PolicyPlanningState policyState)
    {
        if (type is not FusionComplexTypeDefinition complexType)
        {
            return selectionSet;
        }

        List<ISelectionNode>? rewritten = null;

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];
            var updatedSelection = RewriteSelectionWithPolicyRequirements(
                selection,
                complexType,
                policyState);

            if (!ReferenceEquals(selection, updatedSelection))
            {
                rewritten ??= [.. selectionSet.Selections.Take(i)];
            }

            rewritten?.Add(updatedSelection);
        }

        var updatedSelectionSet = rewritten is null
            ? selectionSet
            : selectionSet.WithSelections(rewritten);

        if (complexType is FusionObjectTypeDefinition
            {
                PolicyApplications.IsDefaultOrEmpty: false
            } objectType)
        {
            foreach (var application in objectType.PolicyApplications)
            {
                foreach (var group in application.Groups)
                {
                    foreach (var name in group)
                    {
                        updatedSelectionSet =
                            MergePolicyRequirements(
                                name,
                                updatedSelectionSet,
                                complexType,
                                policyState);
                    }
                }
            }
        }

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode fieldNode
                || !complexType.Fields.TryGetField(
                    fieldNode.Name.Value,
                    allowInaccessibleFields: true,
                    out var field)
                || field.PolicyApplications.IsDefaultOrEmpty)
            {
                continue;
            }

            foreach (var application in field.PolicyApplications)
            {
                foreach (var group in application.Groups)
                {
                    foreach (var name in group)
                    {
                        updatedSelectionSet =
                            MergePolicyRequirements(
                                name,
                                updatedSelectionSet,
                                complexType,
                                policyState);
                    }
                }
            }
        }

        return updatedSelectionSet;
    }

    private ISelectionNode RewriteSelectionWithPolicyRequirements(
        ISelectionNode selection,
        FusionComplexTypeDefinition type,
        PolicyPlanningState policyState)
    {
        switch (selection)
        {
            case FieldNode { SelectionSet: { } childSelectionSet } fieldNode:
            {
                var field = type.Fields.GetField(
                    fieldNode.Name.Value,
                    allowInaccessibleFields: true);
                var rewritten = RewriteSelectionSetWithPolicyRequirements(
                    childSelectionSet,
                    field.Type.NamedType(),
                    policyState);

                return ReferenceEquals(rewritten, childSelectionSet)
                    ? fieldNode
                    : fieldNode.WithSelectionSet(rewritten);
            }

            case InlineFragmentNode inlineFragment:
            {
                var fragmentType = inlineFragment.TypeCondition is null
                    ? type
                    : _schema.Types.GetType(
                        inlineFragment.TypeCondition.Name.Value,
                        allowInaccessibleFields: true);
                var rewritten = RewriteSelectionSetWithPolicyRequirements(
                    inlineFragment.SelectionSet,
                    fragmentType,
                    policyState);

                return ReferenceEquals(rewritten, inlineFragment.SelectionSet)
                    ? inlineFragment
                    : inlineFragment.WithSelectionSet(rewritten);
            }

            default:
                return selection;
        }
    }

    private SelectionSetNode MergePolicyRequirements(
        string policyName,
        SelectionSetNode selectionSet,
        FusionComplexTypeDefinition type,
        PolicyPlanningState policyState)
    {
        if (!policyState.Requirements.TryGetValue(policyName, out var requirements))
        {
            return selectionSet;
        }

        var selections = new List<ISelectionNode>(selectionSet.Selections);
        var changed = false;

        foreach (var requirement in requirements.Selections)
        {
            if (requirement is not FieldNode requirementField)
            {
                throw new InvalidOperationException(
                    $"Authorization policy '{policyName}' has an unsupported requirement selection.");
            }

            if (!type.Fields.TryGetField(
                requirementField.Name.Value,
                allowInaccessibleFields: true,
                out var requirementFieldDefinition))
            {
                throw new InvalidOperationException(
                    $"Authorization policy '{policyName}' requires unknown field "
                    + $"'{type.Name}.{requirementField.Name.Value}'.");
            }

            EnsureRequirementFieldIsNotProtected(
                policyName,
                type,
                requirementFieldDefinition);

            if (requirementField.SelectionSet is { } requirementChildren)
            {
                ValidateRequirementSelectionSet(
                    policyName,
                    requirementChildren,
                    requirementFieldDefinition.Type.NamedType());
            }

            var matchIndex = FindMatchingField(selections, requirementField);

            if (matchIndex < 0)
            {
                selections.Add(MarkRequirement(requirementField));
                changed = true;
                continue;
            }

            if (requirementField.SelectionSet is not { } childRequirements)
            {
                continue;
            }

            var existingField = (FieldNode)selections[matchIndex];
            var existingChildren = existingField.SelectionSet ?? new SelectionSetNode([]);
            var mergedChildren = MergeRequirementSelectionSet(
                policyName,
                existingChildren,
                childRequirements,
                requirementFieldDefinition.Type.NamedType());

            if (!ReferenceEquals(existingChildren, mergedChildren))
            {
                selections[matchIndex] = existingField.WithSelectionSet(mergedChildren);
                changed = true;
            }
        }

        return changed ? selectionSet.WithSelections(selections) : selectionSet;
    }

    private void ValidateRequirementSelectionSet(
        string policyName,
        SelectionSetNode requirements,
        ITypeDefinition type)
    {
        if (type is not FusionComplexTypeDefinition complexType)
        {
            throw new InvalidOperationException(
                $"Authorization policy '{policyName}' selects fields below leaf type '{type.Name}'.");
        }

        foreach (var requirement in requirements.Selections)
        {
            if (requirement is not FieldNode requirementField)
            {
                throw new InvalidOperationException(
                    $"Authorization policy '{policyName}' has an unsupported requirement selection.");
            }

            if (!complexType.Fields.TryGetField(
                requirementField.Name.Value,
                allowInaccessibleFields: true,
                out var requirementFieldDefinition))
            {
                throw new InvalidOperationException(
                    $"Authorization policy '{policyName}' requires unknown field "
                    + $"'{complexType.Name}.{requirementField.Name.Value}'.");
            }

            EnsureRequirementFieldIsNotProtected(
                policyName,
                complexType,
                requirementFieldDefinition);

            if (requirementField.SelectionSet is { } childRequirements)
            {
                ValidateRequirementSelectionSet(
                    policyName,
                    childRequirements,
                    requirementFieldDefinition.Type.NamedType());
            }
        }
    }

    private SelectionSetNode MergeRequirementSelectionSet(
        string policyName,
        SelectionSetNode selectionSet,
        SelectionSetNode requirements,
        ITypeDefinition type)
    {
        if (type is not FusionComplexTypeDefinition complexType)
        {
            throw new InvalidOperationException(
                $"Authorization policy '{policyName}' selects fields below leaf type '{type.Name}'.");
        }

        var selections = new List<ISelectionNode>(selectionSet.Selections);
        var changed = false;

        foreach (var requirement in requirements.Selections)
        {
            if (requirement is not FieldNode requirementField)
            {
                throw new InvalidOperationException(
                    $"Authorization policy '{policyName}' has an unsupported requirement selection.");
            }

            if (!complexType.Fields.TryGetField(
                requirementField.Name.Value,
                allowInaccessibleFields: true,
                out var requirementFieldDefinition))
            {
                throw new InvalidOperationException(
                    $"Authorization policy '{policyName}' requires unknown field "
                    + $"'{complexType.Name}.{requirementField.Name.Value}'.");
            }

            EnsureRequirementFieldIsNotProtected(
                policyName,
                complexType,
                requirementFieldDefinition);

            var matchIndex = FindMatchingField(selections, requirementField);

            if (matchIndex < 0)
            {
                selections.Add(MarkRequirement(requirementField));
                changed = true;
                continue;
            }

            if (requirementField.SelectionSet is not { } childRequirements)
            {
                continue;
            }

            var existingField = (FieldNode)selections[matchIndex];
            var existingChildren = existingField.SelectionSet ?? new SelectionSetNode([]);
            var mergedChildren = MergeRequirementSelectionSet(
                policyName,
                existingChildren,
                childRequirements,
                requirementFieldDefinition.Type.NamedType());

            if (!ReferenceEquals(existingChildren, mergedChildren))
            {
                selections[matchIndex] = existingField.WithSelectionSet(mergedChildren);
                changed = true;
            }
        }

        return changed ? selectionSet.WithSelections(selections) : selectionSet;
    }

    private static int FindMatchingField(
        IReadOnlyList<ISelectionNode> selections,
        FieldNode requirement)
    {
        for (var i = 0; i < selections.Count; i++)
        {
            if (selections[i] is FieldNode { Alias: null } candidate
                && candidate.Name.Value.Equals(requirement.Name.Value, StringComparison.Ordinal)
                && ArgumentsEqual(candidate.Arguments, requirement.Arguments)
                && DirectivesEqual(candidate.Directives, requirement.Directives))
            {
                return i;
            }
        }

        return -1;

        static bool ArgumentsEqual(
            IReadOnlyList<ArgumentNode> left,
            IReadOnlyList<ArgumentNode> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!SyntaxComparer.BySyntax.Equals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        static bool DirectivesEqual(
            IReadOnlyList<DirectiveNode> left,
            IReadOnlyList<DirectiveNode> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!SyntaxComparer.BySyntax.Equals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private static void EnsureRequirementFieldIsNotProtected(
        string policyName,
        ITypeDefinition declaringType,
        FusionOutputFieldDefinition field)
    {
        if (!field.PolicyApplications.IsDefaultOrEmpty)
        {
            throw new InvalidOperationException(
                $"Authorization policy '{policyName}' requires protected field "
                + $"'{declaringType.Name}.{field.Name}', which would create an authorization cycle.");
        }
    }

    private static FieldNode MarkRequirement(FieldNode field)
    {
        var directives = new List<DirectiveNode>(field.Directives.Count + 1);
        directives.AddRange(field.Directives);

        if (!directives.Any(t => t.Name.Value.Equals(RequirementDirectiveName, StringComparison.Ordinal)))
        {
            directives.Add(new DirectiveNode(RequirementDirectiveName));
        }

        var selectionSet = field.SelectionSet;

        if (selectionSet is not null)
        {
            selectionSet = new SelectionSetNode(
                selectionSet.Selections.Select(MarkRequirementSelection).ToArray());
        }

        return field
            .WithDirectives(directives)
            .WithSelectionSet(selectionSet);
    }

    private static ISelectionNode MarkRequirementSelection(ISelectionNode selection)
        => selection switch
        {
            FieldNode field => MarkRequirement(field),
            _ => throw new InvalidOperationException(
                "Authorization policy requirements currently support field selections only.")
        };
}
