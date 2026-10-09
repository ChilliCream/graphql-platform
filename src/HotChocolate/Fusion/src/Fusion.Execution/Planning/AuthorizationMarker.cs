using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Authorization;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Rewriters;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using OperationSelectionSet = HotChocolate.Fusion.Execution.Nodes.SelectionSet;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Marks the deniable selections of the source schema requests of one operation with synthetic
/// <c>@skip(if: $__fusion_auth_N)</c> directives and promotes the variables of fully deniable
/// requests to execution node conditions.
/// </summary>
internal sealed class AuthorizationMarker
{
    private readonly FusionSchemaDefinition _schema;
    private readonly AuthorizationPlanContext _context;
    private readonly Operation _operation;
    private readonly ImmutableArray<PolicyDescriptor> _descriptors;
    private readonly List<AuthorizationVariable> _variables = [];
    private readonly Dictionary<string, AuthorizationVariable> _variablesByKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AuthorizationVariable> _variablesByName = new(StringComparer.Ordinal);
    private readonly Dictionary<int, AuthorizationVariable> _requirementGates = [];

    private AuthorizationMarker(
        FusionSchemaDefinition schema,
        AuthorizationPlanContext context,
        Operation operation,
        ImmutableArray<PolicyDescriptor> descriptors)
    {
        _schema = schema;
        _context = context;
        _operation = operation;
        _descriptors = descriptors;
    }

    /// <summary>
    /// Gets a value indicating whether the marker allocated synthetic variables.
    /// </summary>
    public bool HasVariables => _variables.Count > 0;

    /// <summary>
    /// Creates a marker for the operation, or returns <c>null</c> if no selection of the
    /// operation is protected.
    /// </summary>
    public static AuthorizationMarker? TryCreate(
        FusionSchemaDefinition schema,
        AuthorizationPlanContext context,
        Operation operation)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(operation);

        var candidates = new HashSet<SelectionSetNode>(ReferenceEqualityComparer.Instance);

        if (!FindProtectedFieldNames(operation.Definition.SelectionSet, context.ProtectedFieldNames, candidates))
        {
            return null;
        }

        var protectedSelections = new List<Selection>();
        CollectProtectedSelections(operation, operation.RootSelectionSet, candidates, protectedSelections);

        if (protectedSelections.Count == 0)
        {
            return null;
        }

        var descriptors = ImmutableArray.CreateBuilder<PolicyDescriptor>();

        foreach (var selection in protectedSelections)
        {
            AddDescriptors(context, selection, descriptors);
        }

        return new AuthorizationMarker(schema, context, operation, descriptors.ToImmutable());
    }

    /// <summary>
    /// Gets the variable definitions of the variables that source schema requests reference.
    /// </summary>
    public IEnumerable<VariableDefinitionNode> GetVariableDefinitions()
    {
        foreach (var variable in _variables)
        {
            if (!variable.Operands.IsEmpty)
            {
                continue;
            }

            yield return new VariableDefinitionNode(
                null,
                new VariableNode(null, new NameNode(variable.Name)),
                description: null,
                new NonNullTypeNode(new NamedTypeNode(SpecScalarNames.Boolean.Name)),
                defaultValue: null,
                []);
        }
    }

    /// <summary>
    /// Creates the authorization description of the operation.
    /// </summary>
    public OperationAuthorization CreateAuthorization()
        => new(_descriptors, [.. _variables]);

    /// <summary>
    /// Marks the operation steps and returns the updated steps.
    /// </summary>
    public ImmutableList<PlanStep> Mark(ImmutableList<PlanStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var analyses = new List<StepAnalysis>();

        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i] is OperationPlanStep operationStep)
            {
                analyses.Add(Analyze(operationStep, i));
            }
        }

        ResolveRequirementGates(analyses);

        var updatedSteps = steps;

        foreach (var analysis in analyses)
        {
            var updated = Rewrite(analysis);

            if (!ReferenceEquals(updated, analysis.Step))
            {
                updatedSteps = updatedSteps.SetItem(analysis.Index, updated);
            }
        }

        return updatedSteps;
    }

    private static bool FindProtectedFieldNames(
        SelectionSetNode selectionSet,
        FrozenSet<string> protectedFieldNames,
        HashSet<SelectionSetNode> candidates)
    {
        var found = false;

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode field)
            {
                found |= protectedFieldNames.Contains(field.Name.Value);

                if (field.SelectionSet is not null)
                {
                    found |= FindProtectedFieldNames(field.SelectionSet, protectedFieldNames, candidates);
                }
            }
            else if (selection is InlineFragmentNode fragment)
            {
                found |= FindProtectedFieldNames(fragment.SelectionSet, protectedFieldNames, candidates);
            }
        }

        if (found)
        {
            candidates.Add(selectionSet);
        }

        return found;
    }

    private static void CollectProtectedSelections(
        Operation operation,
        OperationSelectionSet selectionSet,
        HashSet<SelectionSetNode> candidates,
        List<Selection> protectedSelections)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection.HasAuthorization)
            {
                protectedSelections.Add(selection);
            }

            if (selection.IsLeaf || !HasCandidateChildren(selection, candidates))
            {
                continue;
            }

            var childSets = new List<OperationSelectionSet>();
            AddChildSelectionSets(operation, selection, childSets);

            foreach (var childSet in childSets)
            {
                CollectProtectedSelections(operation, childSet, candidates, protectedSelections);
            }
        }
    }

    private static bool HasCandidateChildren(Selection selection, HashSet<SelectionSetNode> candidates)
    {
        foreach (var syntaxNode in selection.SyntaxNodes)
        {
            if (syntaxNode.Node.SelectionSet is { } selectionSet && candidates.Contains(selectionSet))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddChildSelectionSets(
        Operation operation,
        Selection selection,
        List<OperationSelectionSet> childSets)
    {
        if (selection.IsLeaf)
        {
            return;
        }

        if (selection.NamedType is IObjectTypeDefinition)
        {
            if (selection.GetSelectionSet() is { } childSet && !childSets.Contains(childSet))
            {
                childSets.Add(childSet);
            }

            return;
        }

        foreach (var possibleType in operation.GetPossibleTypes(selection))
        {
            if (possibleType is FusionComplexTypeDefinition complexType
                && selection.GetSelectionSet(complexType) is { } childSet
                && !childSets.Contains(childSet))
            {
                childSets.Add(childSet);
            }
        }
    }

    private static void AddDescriptors(
        AuthorizationPlanContext context,
        Selection selection,
        ImmutableArray<PolicyDescriptor>.Builder descriptors)
    {
        var authorization = ((FusionOutputFieldDefinition)selection.Field).Authorization!;

        if (authorization.Authenticated)
        {
            descriptors.Add(
                new PolicyDescriptor(
                    DirectiveNames.Authenticated.Name,
                    policyName: null,
                    scopes: default,
                    selection,
                    context.GetPolicy(DirectiveNames.Authenticated.Name, null)));
        }

        if (authorization.Scopes.Length > 0)
        {
            descriptors.Add(
                new PolicyDescriptor(
                    DirectiveNames.RequiresScopes.Name,
                    policyName: null,
                    authorization.Scopes,
                    selection,
                    context.GetPolicy(DirectiveNames.RequiresScopes.Name, null)));
        }

        var policyNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in authorization.Policies)
        {
            foreach (var policyName in group)
            {
                if (policyNames.Add(policyName))
                {
                    descriptors.Add(
                        new PolicyDescriptor(
                            DirectiveNames.Policy.Name,
                            policyName,
                            scopes: default,
                            selection,
                            context.GetPolicy(DirectiveNames.Policy.Name, policyName)));
                }
            }
        }
    }

    private StepAnalysis Analyze(OperationPlanStep step, int index)
    {
        var contexts = ResolveContexts(step.Target);

        if (contexts is null
            || !TryGetSelectionSet(step.Definition.SelectionSet, step.Source, 0, out var dataSet))
        {
            return new StepAnalysis(step, index, null, null);
        }

        var analysis = new StepAnalysis(step, index, dataSet, contexts);
        AnalyzeSelectionSet(dataSet, contexts, analysis, true);
        return analysis;
    }

    private void AnalyzeSelectionSet(
        SelectionSetNode selectionSet,
        List<OperationSelectionSet> contexts,
        StepAnalysis analysis,
        bool isRoot)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode field)
            {
                var selections = GetSelections(field, contexts);
                var variable = GetVariable(selections);

                if (isRoot)
                {
                    analysis.AddRootVariable(variable);
                }

                if (field.SelectionSet is not null)
                {
                    AnalyzeSelectionSet(field.SelectionSet, GetChildContexts(selections), analysis, false);
                }

                if (analysis.Step.Requirements.Count > 0)
                {
                    analysis.AddUsages(field, variable);
                }
            }
            else if (selection is InlineFragmentNode fragment)
            {
                var isTransparent = isRoot && fragment.TypeCondition is null;

                if (isRoot && !isTransparent)
                {
                    analysis.MarkNotUniformlyGated();
                }

                AnalyzeSelectionSet(
                    fragment.SelectionSet,
                    NarrowContexts(fragment.TypeCondition, contexts),
                    analysis,
                    isTransparent);
            }
            else if (isRoot)
            {
                analysis.MarkNotUniformlyGated();
            }
        }
    }

    private void ResolveRequirementGates(List<StepAnalysis> analyses)
    {
        _requirementGates.Clear();

        // No requirement selection is exclusive when the operation has deferred parts.
        if (_operation.HasIncrementalParts)
        {
            return;
        }

        var consumers = new Dictionary<int, (int Count, AuthorizationVariable? Gate)>();
        var selectionsById = new Dictionary<int, Selection>();

        foreach (var analysis in analyses)
        {
            foreach (var requirement in analysis.Step.Requirements.Values)
            {
                var selections = ResolveRequirementSelections(requirement);

                if (selections is null)
                {
                    return;
                }

                var gate = analysis.GetGate(requirement.Key);

                foreach (var selection in selections)
                {
                    selectionsById[selection.Id] = selection;
                    consumers[selection.Id] = consumers.TryGetValue(selection.Id, out var existing)
                        ? (existing.Count + 1, null)
                        : (1, gate);
                }
            }
        }

        foreach (var (id, consumer) in consumers)
        {
            if (consumer is { Count: 1, Gate: { } gate } && selectionsById[id].IsInternal)
            {
                _requirementGates.Add(id, gate);
            }
        }
    }

    private List<Selection>? ResolveRequirementSelections(OperationRequirement requirement)
    {
        var contexts = ResolveContexts(requirement.Path);

        if (contexts is null)
        {
            return null;
        }

        var map = ValueSelectionToSelectionSetRewriter.Rewrite([requirement.Map]);
        var selections = new List<Selection>();

        return CollectRequirementSelections(map, contexts, requirement.InternalAlias, selections)
            ? selections
            : null;
    }

    private bool CollectRequirementSelections(
        SelectionSetNode selectionSet,
        List<OperationSelectionSet> contexts,
        string? internalAlias,
        List<Selection> selections)
    {
        if (internalAlias is not null && selectionSet.Selections.Count != 1)
        {
            return false;
        }

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode field)
            {
                var responseName = internalAlias ?? field.Alias?.Value ?? field.Name.Value;
                var children = new List<Selection>();

                foreach (var context in contexts)
                {
                    if (context.TryGetSelection(responseName, out var match))
                    {
                        if (!selections.Contains(match))
                        {
                            selections.Add(match);
                        }

                        children.Add(match);
                    }
                }

                if (children.Count == 0)
                {
                    return false;
                }

                if (field.SelectionSet is not null
                    && !CollectRequirementSelections(
                        field.SelectionSet,
                        GetChildContexts(children),
                        internalAlias: null,
                        selections))
                {
                    return false;
                }
            }
            else if (selection is InlineFragmentNode fragment
                && !CollectRequirementSelections(
                    fragment.SelectionSet,
                    NarrowContexts(fragment.TypeCondition, contexts),
                    internalAlias,
                    selections))
            {
                return false;
            }
        }

        return true;
    }

    private OperationPlanStep Rewrite(StepAnalysis analysis)
    {
        var step = analysis.Step;

        if (analysis.DataSet is null || analysis.Contexts is null)
        {
            return step;
        }

        var dataSet = RewriteSelectionSet(analysis.DataSet, analysis.Contexts);
        var conditions = step.Conditions;

        if (TryCreateNodeCondition(dataSet, out var nodeCondition))
        {
            conditions =
            [
                .. conditions,
                nodeCondition
            ];
            Array.Sort(conditions, static (a, b) => string.CompareOrdinal(a.VariableName, b.VariableName));
        }

        if (ReferenceEquals(dataSet, analysis.DataSet) && conditions.Length == step.Conditions.Length)
        {
            return step;
        }

        var selectionSet = ReplaceSelectionSet(step.Definition.SelectionSet, step.Source, 0, dataSet);

        return step with
        {
            Definition = step.Definition.WithSelectionSet(selectionSet),
            Conditions = conditions
        };
    }

    private SelectionSetNode RewriteSelectionSet(
        SelectionSetNode selectionSet,
        List<OperationSelectionSet> contexts)
    {
        List<ISelectionNode>? rewritten = null;

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];
            var replacement = selection;

            if (selection is FieldNode field)
            {
                replacement = RewriteField(field, contexts);
            }
            else if (selection is InlineFragmentNode fragment)
            {
                var rewrittenSet = RewriteSelectionSet(
                    fragment.SelectionSet,
                    NarrowContexts(fragment.TypeCondition, contexts));

                if (!ReferenceEquals(rewrittenSet, fragment.SelectionSet))
                {
                    replacement = fragment.WithSelectionSet(rewrittenSet);
                }
            }

            if (rewritten is null)
            {
                if (ReferenceEquals(replacement, selection))
                {
                    continue;
                }

                rewritten = [.. selectionSet.Selections.Take(i)];
            }

            rewritten.Add(replacement);
        }

        return rewritten is null ? selectionSet : selectionSet.WithSelections(rewritten);
    }

    private ISelectionNode RewriteField(FieldNode field, List<OperationSelectionSet> contexts)
    {
        var selections = GetSelections(field, contexts);
        var rewritten = field;

        if (field.SelectionSet is not null)
        {
            var childSet = RewriteSelectionSet(field.SelectionSet, GetChildContexts(selections));

            if (!ReferenceEquals(childSet, field.SelectionSet))
            {
                rewritten = field.WithSelectionSet(childSet);
            }
        }

        var variables = new List<AuthorizationVariable>(2);

        if (GetVariable(selections) is { } ownVariable)
        {
            variables.Add(ownVariable);
        }

        foreach (var selection in selections)
        {
            if (_requirementGates.TryGetValue(selection.Id, out var gate) && !variables.Contains(gate))
            {
                variables.Add(gate);
            }
        }

        ISelectionNode result = rewritten;

        foreach (var variable in variables)
        {
            result = AddSkip(result, variable);
        }

        return result;
    }

    private bool TryCreateNodeCondition(
        SelectionSetNode dataSet,
        out ExecutionNodeCondition condition)
    {
        condition = null!;

        var rootVariables = new List<List<AuthorizationVariable>>();

        if (!TryCollectRootVariables(dataSet, [], rootVariables) || rootVariables.Count == 0)
        {
            return false;
        }

        var common = new HashSet<AuthorizationVariable>(rootVariables[0]);

        for (var i = 1; i < rootVariables.Count && common.Count > 0; i++)
        {
            common.IntersectWith(rootVariables[i]);
        }

        // A variable shared by all root selections becomes the node condition through the
        // common condition extraction.
        if (common.Count > 0)
        {
            return false;
        }

        var operands = new List<AuthorizationVariable>();

        foreach (var variables in rootVariables)
        {
            var first = variables.OrderBy(static v => v.Name, StringComparer.Ordinal).First();

            if (!operands.Contains(first))
            {
                operands.Add(first);
            }
        }

        operands.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));

        var key = "node:" + string.Join(",", operands.Select(static o => o.Name));

        if (!_variablesByKey.TryGetValue(key, out var nodeVariable))
        {
            nodeVariable = Register(key, selections: [], [.. operands]);
        }

        condition = new ExecutionNodeCondition
        {
            VariableName = nodeVariable.Name,
            PassingValue = false
        };
        return true;
    }

    private bool TryCollectRootVariables(
        SelectionSetNode selectionSet,
        List<AuthorizationVariable> inherited,
        List<List<AuthorizationVariable>> rootVariables)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode field)
            {
                var variables = GetSkipVariables(field.Directives, inherited);

                if (variables.Count == 0)
                {
                    return false;
                }

                rootVariables.Add(variables);
            }
            else if (selection is InlineFragmentNode { TypeCondition: null } fragment)
            {
                if (!TryCollectRootVariables(
                    fragment.SelectionSet,
                    GetSkipVariables(fragment.Directives, inherited),
                    rootVariables))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private List<AuthorizationVariable> GetSkipVariables(
        IReadOnlyList<DirectiveNode> directives,
        List<AuthorizationVariable> inherited)
    {
        var variables = new List<AuthorizationVariable>(inherited);

        foreach (var directive in directives)
        {
            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal)
                && directive.Arguments is [{ Value: VariableNode variableNode }]
                && _variablesByName.TryGetValue(variableNode.Name.Value, out var variable)
                && !variables.Contains(variable))
            {
                variables.Add(variable);
            }
        }

        return variables;
    }

    private static ISelectionNode AddSkip(ISelectionNode selection, AuthorizationVariable variable)
    {
        var directive = new DirectiveNode(
            DirectiveNames.Skip.Name,
            new ArgumentNode(
                DirectiveNames.Skip.Arguments.If,
                new VariableNode(null, new NameNode(variable.Name))));

        // A selection that already carries @skip is wrapped in an inline fragment.
        if (selection is FieldNode field && !HasSkip(field.Directives))
        {
            return field.WithDirectives([.. field.Directives, directive]);
        }

        return new InlineFragmentNode(
            null,
            null,
            [directive],
            new SelectionSetNode([selection]));
    }

    private static bool HasSkip(IReadOnlyList<DirectiveNode> directives)
    {
        foreach (var directive in directives)
        {
            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private AuthorizationVariable? GetVariable(List<Selection> selections)
    {
        List<Selection>? protectedSelections = null;

        foreach (var selection in selections)
        {
            if (selection.HasAuthorization)
            {
                (protectedSelections ??= []).Add(selection);
            }
        }

        if (protectedSelections is null)
        {
            return null;
        }

        protectedSelections.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        var key = "selections:" + string.Join(",", protectedSelections.Select(static s => s.Id));

        return _variablesByKey.TryGetValue(key, out var variable)
            ? variable
            : Register(key, [.. protectedSelections], []);
    }

    private AuthorizationVariable Register(
        string key,
        ImmutableArray<Selection> selections,
        ImmutableArray<AuthorizationVariable> operands)
    {
        var variable = new AuthorizationVariable(_context.NextVariableName(), selections, operands);
        _variables.Add(variable);
        _variablesByKey.Add(key, variable);
        _variablesByName.Add(variable.Name, variable);
        return variable;
    }

    private List<Selection> GetSelections(FieldNode field, List<OperationSelectionSet> contexts)
    {
        var responseName = field.Alias?.Value ?? field.Name.Value;
        var selections = new List<Selection>(contexts.Count);

        foreach (var context in contexts)
        {
            if (context.TryGetSelection(responseName, out var selection) && !selections.Contains(selection))
            {
                selections.Add(selection);
            }
        }

        return selections;
    }

    private List<OperationSelectionSet> GetChildContexts(List<Selection> selections)
    {
        var childSets = new List<OperationSelectionSet>();

        foreach (var selection in selections)
        {
            AddChildSelectionSets(_operation, selection, childSets);
        }

        return childSets;
    }

    private List<OperationSelectionSet> NarrowContexts(NamedTypeNode? typeCondition, List<OperationSelectionSet> contexts)
    {
        if (typeCondition is null)
        {
            return contexts;
        }

        var narrowed = new List<OperationSelectionSet>(contexts.Count);
        _schema.Types.TryGetType(typeCondition.Name.Value, allowInaccessibleFields: true, out var type);

        foreach (var context in contexts)
        {
            if (typeCondition.Name.Value.Equals(context.Type.Name, StringComparison.Ordinal)
                || type?.IsAssignableFrom(context.Type) == true)
            {
                narrowed.Add(context);
            }
        }

        return narrowed;
    }

    private List<OperationSelectionSet>? ResolveContexts(SelectionPath path)
    {
        var contexts = new List<OperationSelectionSet> { _operation.RootSelectionSet };

        for (var i = 0; i < path.Length; i++)
        {
            var segment = path[i];

            if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
            {
                contexts = NarrowContexts(new NamedTypeNode(segment.Name), contexts);
            }
            else
            {
                var selections = new List<Selection>(contexts.Count);

                foreach (var context in contexts)
                {
                    if (context.TryGetSelection(segment.Name, out var selection)
                        && !selections.Contains(selection))
                    {
                        selections.Add(selection);
                    }
                }

                contexts = GetChildContexts(selections);
            }

            if (contexts.Count == 0)
            {
                return null;
            }
        }

        return contexts;
    }

    private static bool TryGetSelectionSet(
        SelectionSetNode current,
        SelectionPath path,
        int index,
        out SelectionSetNode selectionSet)
    {
        while (index < path.Length)
        {
            if (!TryFindChild(current, path[index], out _, out var child))
            {
                selectionSet = null!;
                return false;
            }

            current = child;
            index++;
        }

        selectionSet = current;
        return true;
    }

    private static SelectionSetNode ReplaceSelectionSet(
        SelectionSetNode current,
        SelectionPath path,
        int index,
        SelectionSetNode replacement)
    {
        if (index == path.Length)
        {
            return replacement;
        }

        if (!TryFindChild(current, path[index], out var node, out var childSet))
        {
            return current;
        }

        var rewrittenChild = ReplaceSelectionSet(childSet, path, index + 1, replacement);

        var updatedNode = node is FieldNode field
            ? (ISelectionNode)field.WithSelectionSet(rewrittenChild)
            : ((InlineFragmentNode)node).WithSelectionSet(rewrittenChild);

        var selections = new List<ISelectionNode>(current.Selections.Count);

        foreach (var selection in current.Selections)
        {
            selections.Add(ReferenceEquals(selection, node) ? updatedNode : selection);
        }

        return current.WithSelections(selections);
    }

    private static bool TryFindChild(
        SelectionSetNode current,
        SelectionPath.Segment segment,
        out ISelectionNode node,
        out SelectionSetNode selectionSet)
    {
        foreach (var selection in current.Selections)
        {
            switch (selection)
            {
                case InlineFragmentNode fragment
                    when segment.Kind is SelectionPathSegmentKind.InlineFragment
                        && fragment.TypeCondition?.Name.Value == segment.Name:
                    node = fragment;
                    selectionSet = fragment.SelectionSet;
                    return true;

                case FieldNode { SelectionSet: { } fieldSet } field
                    when segment.Kind is SelectionPathSegmentKind.Field
                        && (field.Alias?.Value == segment.Name || field.Name.Value == segment.Name):
                    node = field;
                    selectionSet = fieldSet;
                    return true;
            }
        }

        node = null!;
        selectionSet = null!;
        return false;
    }

    private sealed class StepAnalysis(
        OperationPlanStep step,
        int index,
        SelectionSetNode? dataSet,
        List<OperationSelectionSet>? contexts)
    {
        private Dictionary<string, List<AuthorizationVariable?>>? _usages;
        private Dictionary<string, int>? _usageCounts;
        private Dictionary<string, int>? _totalCounts;
        private List<AuthorizationVariable>? _rootVariables;
        private bool _isUniformlyGated = true;

        public OperationPlanStep Step { get; } = step;

        public int Index { get; } = index;

        public SelectionSetNode? DataSet { get; } = dataSet;

        public List<OperationSelectionSet>? Contexts { get; } = contexts;

        public void AddRootVariable(AuthorizationVariable? variable)
        {
            if (variable is null)
            {
                _isUniformlyGated = false;
            }
            else
            {
                (_rootVariables ??= []).Add(variable);
            }
        }

        public void MarkNotUniformlyGated() => _isUniformlyGated = false;

        public void AddUsages(FieldNode field, AuthorizationVariable? variable)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var argument in field.Arguments)
            {
                VariableUsageCounter.Instance.Visit(argument, counts);
            }

            foreach (var (name, count) in counts)
            {
                if (!Step.Requirements.ContainsKey(name))
                {
                    continue;
                }

                _usages ??= new Dictionary<string, List<AuthorizationVariable?>>(StringComparer.Ordinal);
                _usageCounts ??= new Dictionary<string, int>(StringComparer.Ordinal);

                if (!_usages.TryGetValue(name, out var variables))
                {
                    variables = [];
                    _usages.Add(name, variables);
                }

                variables.Add(variable);
                _usageCounts[name] = _usageCounts.GetValueOrDefault(name) + count;
            }
        }

        /// <summary>
        /// Gets the variable that gates the single use of the requirement variable and every root
        /// selection of the step, or <c>null</c> if no such variable exists.
        /// </summary>
        public AuthorizationVariable? GetGate(string requirementKey)
        {
            if (_usages is null
                || _usageCounts is null
                || !_usages.TryGetValue(requirementKey, out var variables))
            {
                return null;
            }

            if (_totalCounts is null)
            {
                _totalCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                VariableUsageCounter.Instance.Visit(Step.Definition, _totalCounts);
            }

            if (_totalCounts.GetValueOrDefault(requirementKey) != _usageCounts[requirementKey])
            {
                return null;
            }

            var gate = variables[0];

            foreach (var variable in variables)
            {
                if (!ReferenceEquals(variable, gate))
                {
                    return null;
                }
            }

            if (!_isUniformlyGated || _rootVariables is null)
            {
                return null;
            }

            foreach (var rootVariable in _rootVariables)
            {
                if (!ReferenceEquals(rootVariable, gate))
                {
                    return null;
                }
            }

            return gate;
        }
    }

    private sealed class VariableUsageCounter : SyntaxWalker<Dictionary<string, int>>
    {
        private VariableUsageCounter()
            : base(new SyntaxVisitorOptions { VisitArguments = true, VisitDirectives = true })
        {
        }

        public static VariableUsageCounter Instance { get; } = new();

        protected override ISyntaxVisitorAction Enter(
            VariableNode node,
            Dictionary<string, int> context)
        {
            var name = node.Name.Value;
            context[name] = context.GetValueOrDefault(name) + 1;
            return base.Enter(node, context);
        }
    }
}
