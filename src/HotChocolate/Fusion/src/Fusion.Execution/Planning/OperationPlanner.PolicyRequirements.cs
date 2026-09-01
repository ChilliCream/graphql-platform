using System.Collections.Immutable;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    private const string RequirementDirectiveName = "fusion__requirement";

    private static PolicyPlanningState CreatePolicyPlanningState(
        ImmutableArray<IPolicy> policies)
    {
        var requirements = new Dictionary<string, SelectionSetNode>(StringComparer.Ordinal);
        var cacheability = new Dictionary<string, bool>(StringComparer.Ordinal);
        var hashes = new Dictionary<string, ulong>(StringComparer.Ordinal);

        foreach (var policy in policies)
        {
            var policyRequirements = policy.Requirements;
            cacheability[policy.Name] = policyRequirements.IsRequestCacheable;
            hashes[policy.Name] = PolicyPlanEntry.ComputeRequirementHash(policyRequirements.Resource);

            if (policyRequirements.Resource is not { } selectionSet)
            {
                continue;
            }

            requirements.Add(policy.Name, selectionSet);
        }

        return new PolicyPlanningState(policies, requirements, cacheability, hashes);
    }

    private ImmutableHashSet<string> CreatePolicyRequirementFeedPaths(
        OperationDefinitionNode operation,
        PolicyPlanningState policyState)
    {
        var feeds = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var path = new List<string>();
        VisitSelectionSet(
            operation.SelectionSet,
            _schema.GetOperationType(operation.Operation),
            path);
        return feeds.ToImmutable();

        void VisitSelectionSet(
            SelectionSetNode selectionSet,
            ITypeDefinition type,
            List<string> currentPath)
        {
            if (type is not FusionComplexTypeDefinition
                && type is not FusionUnionTypeDefinition)
            {
                return;
            }

            var complexType = type as FusionComplexTypeDefinition;
            AddObjectRequirements(type, currentPath);

            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode fieldNode:
                        if (complexType is null
                            || !complexType.Fields.TryGetField(
                            fieldNode.Name.Value,
                            allowInaccessibleFields: true,
                            out var field))
                        {
                            continue;
                        }

                        AddRequirements(field.PolicyApplications, currentPath);
                        currentPath.Add(fieldNode.Alias?.Value ?? fieldNode.Name.Value);

                        AddObjectRequirements(field.Type.NamedType(), currentPath);

                        if (fieldNode.SelectionSet is { } childSelectionSet)
                        {
                            VisitSelectionSet(childSelectionSet, field.Type.NamedType(), currentPath);
                        }

                        currentPath.RemoveAt(currentPath.Count - 1);
                        break;

                    case InlineFragmentNode fragment:
                        var fragmentType = fragment.TypeCondition is null
                            ? type
                            : _schema.Types.GetType(
                                fragment.TypeCondition.Name.Value,
                                allowInaccessibleFields: true);
                        VisitSelectionSet(fragment.SelectionSet, fragmentType, currentPath);
                        break;
                }
            }
        }

        void AddObjectRequirements(
            ITypeDefinition type,
            IReadOnlyList<string> entityPath)
        {
            if (type is FusionObjectTypeDefinition objectType)
            {
                AddRequirements(objectType.PolicyApplications, entityPath);
                return;
            }

            if (type is not FusionInterfaceTypeDefinition and not FusionUnionTypeDefinition)
            {
                return;
            }

            foreach (var possibleType in _schema.GetPossibleTypes(type, includeInaccessible: true))
            {
                AddRequirements(possibleType.PolicyApplications, entityPath);
            }
        }

        void AddRequirements(
            ImmutableArray<PolicyApplication> applications,
            IReadOnlyList<string> entityPath)
        {
            if (applications.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var application in applications)
            {
                foreach (var group in application.Groups)
                {
                    foreach (var name in group)
                    {
                        if (policyState.Requirements.TryGetValue(name, out var requirements))
                        {
                            AddRequirementFields(requirements, entityPath);
                        }
                    }
                }
            }
        }

        void AddRequirementFields(
            SelectionSetNode requirements,
            IReadOnlyList<string> entityPath)
        {
            var requirementPath = new List<string>(entityPath);
            VisitRequirements(requirements, requirementPath);
        }

        void VisitRequirements(SelectionSetNode requirements, List<string> requirementPath)
        {
            foreach (var selection in requirements.Selections)
            {
                if (selection is not FieldNode field)
                {
                    continue;
                }

                requirementPath.Add(field.Alias?.Value ?? field.Name.Value);
                feeds.Add(CreatePolicyPathKey(requirementPath));

                if (field.SelectionSet is { } children)
                {
                    VisitRequirements(children, requirementPath);
                }

                requirementPath.RemoveAt(requirementPath.Count - 1);
            }
        }
    }

    /// <summary>
    /// Gets whether the named policy is known and produces a request-constant decision.
    /// Unknown policy names return <c>false</c>.
    /// </summary>
    private static bool IsPolicyRequestCacheable(string policyName, PolicyPlanningState policyState)
        => policyState.RequestCacheability.TryGetValue(policyName, out var cacheable) && cacheable;

    /// <summary>
    /// Gets whether every policy name in <paramref name="application"/> is request-cacheable.
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
    /// Classifies an application as request-constant, mixed, or residual-only.
    /// </summary>
    private static PolicyApplicationClass ClassifyApplication(
        PolicyApplication application,
        PolicyPlanningState policyState)
    {
        if (IsApplicationRequestCacheable(application, policyState))
        {
            return PolicyApplicationClass.S;
        }

        var hasRequestCacheable = false;
        foreach (var group in application.Groups)
        {
            var groupHasRequestCacheable = false;
            foreach (var name in group)
            {
                if (IsPolicyRequestCacheable(name, policyState))
                {
                    hasRequestCacheable = true;
                    groupHasRequestCacheable = true;
                }
            }

            if (!groupHasRequestCacheable)
            {
                return PolicyApplicationClass.ResidualOnly;
            }
        }

        return hasRequestCacheable
            ? PolicyApplicationClass.M
            : PolicyApplicationClass.ResidualOnly;
    }

    /// <summary>
    /// Defines the execution classification of a policy application.
    /// </summary>
    private enum PolicyApplicationClass
    {
        /// <summary>Every policy name in the application is request-cacheable.</summary>
        S,

        /// <summary>Every arm mixes request-cacheable and data-bearing names.</summary>
        M,

        /// <summary>The application has no safe non-tautological request projection.</summary>
        ResidualOnly
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
        if (type is not FusionComplexTypeDefinition
            && type is not FusionUnionTypeDefinition)
        {
            return selectionSet;
        }

        List<ISelectionNode>? rewritten = null;

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];
            var updatedSelection = RewriteSelectionWithPolicyRequirements(
                selection,
                type,
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

        if (type is FusionObjectTypeDefinition
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
                                objectType,
                                policyState);
                    }
                }
            }
        }
        else if (type is FusionInterfaceTypeDefinition or FusionUnionTypeDefinition)
        {
            updatedSelectionSet = MergePossibleTypePolicyRequirements(
                updatedSelectionSet,
                type,
                policyState);
        }

        if (type is not FusionComplexTypeDefinition complexType)
        {
            return updatedSelectionSet;
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

    private SelectionSetNode MergePossibleTypePolicyRequirements(
        SelectionSetNode selectionSet,
        ITypeDefinition abstractType,
        PolicyPlanningState policyState)
    {
        var selections = new List<ISelectionNode>(selectionSet.Selections);

        foreach (var possibleType in _schema.GetPossibleTypes(
            abstractType,
            includeInaccessible: true).OrderBy(type => type.Name, StringComparer.Ordinal))
        {
            if (possibleType.PolicyApplications.IsDefaultOrEmpty)
            {
                continue;
            }

            var requirements = new SelectionSetNode([]);

            foreach (var application in possibleType.PolicyApplications)
            {
                foreach (var group in application.Groups)
                {
                    foreach (var name in group)
                    {
                        requirements = MergePolicyRequirements(
                            name,
                            requirements,
                            possibleType,
                            policyState);
                    }
                }
            }

            if (requirements.Selections.Count == 0)
            {
                continue;
            }

            selections.Add(
                new InlineFragmentNode(
                    location: null,
                    new NamedTypeNode(possibleType.Name),
                    directives: [],
                    requirements));
        }

        return selections.Count == selectionSet.Selections.Count
            ? selectionSet
            : selectionSet.WithSelections(selections);
    }

    private ISelectionNode RewriteSelectionWithPolicyRequirements(
        ISelectionNode selection,
        ITypeDefinition type,
        PolicyPlanningState policyState)
    {
        switch (selection)
        {
            case FieldNode { SelectionSet: { } childSelectionSet } fieldNode:
            {
                if (type is not FusionComplexTypeDefinition complexType)
                {
                    return selection;
                }

                var field = complexType.Fields.GetField(
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

            EnsureRequirementFieldHasNoDataBearingPolicy(
                policyName,
                type,
                requirementFieldDefinition,
                policyState);

            if (requirementField.SelectionSet is { } requirementChildren)
            {
                ValidateRequirementSelectionSet(
                    policyName,
                    requirementChildren,
                    requirementFieldDefinition.Type.NamedType(),
                    policyState);
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
                requirementFieldDefinition.Type.NamedType(),
                policyState);

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
        ITypeDefinition type,
        PolicyPlanningState policyState)
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

            EnsureRequirementFieldHasNoDataBearingPolicy(
                policyName,
                complexType,
                requirementFieldDefinition,
                policyState);

            if (requirementField.SelectionSet is { } childRequirements)
            {
                ValidateRequirementSelectionSet(
                    policyName,
                    childRequirements,
                    requirementFieldDefinition.Type.NamedType(),
                    policyState);
            }
        }
    }

    private SelectionSetNode MergeRequirementSelectionSet(
        string policyName,
        SelectionSetNode selectionSet,
        SelectionSetNode requirements,
        ITypeDefinition type,
        PolicyPlanningState policyState)
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

            EnsureRequirementFieldHasNoDataBearingPolicy(
                policyName,
                complexType,
                requirementFieldDefinition,
                policyState);

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
                requirementFieldDefinition.Type.NamedType(),
                policyState);

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

    private static void EnsureRequirementFieldHasNoDataBearingPolicy(
        string policyName,
        ITypeDefinition declaringType,
        FusionOutputFieldDefinition field,
        PolicyPlanningState policyState)
    {
        if (!field.PolicyApplications.IsDefaultOrEmpty
            && field.PolicyApplications.Any(
                application => !IsApplicationRequestCacheable(application, policyState)))
        {
            throw Execution.ThrowHelper.PolicyRequirementAuthorizationCycle(
                policyName,
                $"{declaringType.Name}.{field.Name}");
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
