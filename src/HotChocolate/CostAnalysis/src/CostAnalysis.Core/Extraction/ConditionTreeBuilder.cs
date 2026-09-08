using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Builds one <see cref="ConditionTree"/> from a boundary's raw selections,
/// hash-consing nodes on their canonical <see cref="Condition"/> so
/// different fragment spellings that reach the same condition share a node.
/// </summary>
internal sealed class ConditionTreeBuilder
{
    private readonly CostSchemaSnapshot _snapshot;
    private readonly IReadOnlyDictionary<string, FragmentDefinitionNode> _fragments;
    private readonly IReadOnlyDictionary<string, bool>? _knownVariableValues;
    private readonly List<ConditionTreeNode> _nodes = [];
    private readonly Dictionary<Condition, int> _nodeIdByCondition = [];
    private readonly Dictionary<(PossibleTypeSet Scope, string TypeName), PossibleTypeSet> _intersections = [];
    private readonly HashSet<string> _fragmentsInProgress = [];

    public ConditionTreeBuilder(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        IReadOnlyDictionary<string, bool>? knownVariableValues)
    {
        _snapshot = snapshot;
        _fragments = fragments;
        _knownVariableValues = knownVariableValues;
    }

    public ConditionTree Build(IReadOnlyList<ISelectionNode> selections, Condition rootCondition)
    {
        var rootNodeId = GetOrCreateNode(rootCondition);
        InsertSelections(selections, rootNodeId, rootCondition);
        return new ConditionTree(rootNodeId, _nodes);
    }

    private void InsertSelections(IReadOnlyList<ISelectionNode> selections, int nodeId, Condition condition)
    {
        foreach (var selection in selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    InsertField(field, nodeId, condition);
                    break;

                case InlineFragmentNode inlineFragment:
                    InsertFragment(
                        inlineFragment.TypeCondition?.Name.Value,
                        inlineFragment.Directives,
                        inlineFragment.SelectionSet.Selections,
                        nodeId,
                        condition);
                    break;

                case FragmentSpreadNode spread:
                    InsertSpread(spread, nodeId, condition);
                    break;
            }
        }
    }

    private void InsertSpread(FragmentSpreadNode spread, int nodeId, Condition condition)
    {
        var fragmentName = spread.Name.Value;

        if (!_fragments.TryGetValue(fragmentName, out var definition)
            || !_fragmentsInProgress.Add(fragmentName))
        {
            // Extraction assumes a validated document: an unresolved name or a
            // fragment cycle cannot occur, but skipping instead of throwing
            // keeps this pass total over whatever it is handed.
            return;
        }

        InsertFragment(
            definition.TypeCondition.Name.Value,
            spread.Directives,
            definition.SelectionSet.Selections,
            nodeId,
            condition);

        _fragmentsInProgress.Remove(fragmentName);
    }

    private void InsertFragment(
        string? typeConditionName,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<ISelectionNode> selections,
        int nodeId,
        Condition condition)
    {
        if (typeConditionName is not null)
        {
            var narrowed = Intersect(condition.PossibleTypes, typeConditionName);

            if (narrowed.Count == 0)
            {
                return;
            }

            if (!narrowed.Equals(condition.PossibleTypes))
            {
                condition = new Condition(narrowed, condition.BooleanCondition);
                nodeId = GetOrCreateEdge(nodeId, BranchCondition.Type(typeConditionName), condition);
            }
        }

        if (!TryApplyDirectives(directives, ref nodeId, ref condition))
        {
            return;
        }

        InsertSelections(selections, nodeId, condition);
    }

    private void InsertField(FieldNode field, int nodeId, Condition condition)
    {
        if (!TryApplyDirectives(field.Directives, ref nodeId, ref condition))
        {
            return;
        }

        _nodes[nodeId].AddField(field);
    }

    /// <summary>
    /// Walks a selection's <c>@include</c>/<c>@skip</c> directives one at a
    /// time, each contributing at most one edge, and returns
    /// <see langword="false"/> when a directive makes the selection
    /// infeasible.
    /// </summary>
    private bool TryApplyDirectives(IReadOnlyList<DirectiveNode> directives, ref int nodeId, ref Condition condition)
    {
        foreach (var directive in directives)
        {
            var outcome = ResolveDirective(directive, out var literal);

            if (outcome == DirectiveOutcome.Infeasible)
            {
                return false;
            }

            if (outcome == DirectiveOutcome.NoOp)
            {
                continue;
            }

            if (IsKnownInactive(literal))
            {
                return false;
            }

            if (!Condition.TryInsertLiteral(condition.BooleanCondition, literal, out var nextBooleanCondition))
            {
                return false;
            }

            if (nextBooleanCondition.Length == condition.BooleanCondition.Length)
            {
                // The literal already held (inherited or from an earlier
                // directive on this same selection): no new edge needed.
                continue;
            }

            condition = new Condition(condition.PossibleTypes, nextBooleanCondition);
            nodeId = GetOrCreateEdge(nodeId, BranchCondition.Boolean(literal), condition);
        }

        return true;
    }

    private bool IsKnownInactive(BooleanLiteral literal)
        => _knownVariableValues is not null
            && _knownVariableValues.TryGetValue(literal.VariableName, out var knownValue)
            && knownValue != literal.IsPositive;

    private enum DirectiveOutcome
    {
        NoOp,
        Infeasible,
        Literal
    }

    private static DirectiveOutcome ResolveDirective(DirectiveNode directive, out BooleanLiteral literal)
    {
        literal = default;
        bool isInclude;

        if (directive.Name.Value == DirectiveNames.Include.Name)
        {
            isInclude = true;
        }
        else if (directive.Name.Value == DirectiveNames.Skip.Name)
        {
            isInclude = false;
        }
        else
        {
            return DirectiveOutcome.NoOp;
        }

        var ifArgumentName = isInclude
            ? DirectiveNames.Include.Arguments.If
            : DirectiveNames.Skip.Arguments.If;
        IValueNode? ifValue = null;

        foreach (var argument in directive.Arguments)
        {
            if (argument.Name.Value == ifArgumentName)
            {
                ifValue = argument.Value;
                break;
            }
        }

        return ifValue switch
        {
            BooleanValueNode booleanValue => booleanValue.Value == isInclude
                ? DirectiveOutcome.NoOp
                : DirectiveOutcome.Infeasible,
            VariableNode variable => SetLiteral(variable.Name.Value, isInclude, out literal),
            // A non-Boolean `if` behaves like false: @include drops the
            // selection, @skip keeps it.
            _ => isInclude ? DirectiveOutcome.Infeasible : DirectiveOutcome.NoOp
        };
    }

    private static DirectiveOutcome SetLiteral(string variableName, bool isPositive, out BooleanLiteral literal)
    {
        literal = new BooleanLiteral(variableName, isPositive);
        return DirectiveOutcome.Literal;
    }

    private PossibleTypeSet Intersect(PossibleTypeSet scope, string typeName)
    {
        var key = (scope, typeName);

        if (_intersections.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = scope.Intersect(_snapshot.GetPossibleTypeSet(typeName));
        _intersections.Add(key, result);
        return result;
    }

    private int GetOrCreateEdge(int fromNodeId, BranchCondition branchCondition, Condition targetCondition)
    {
        var targetNodeId = GetOrCreateNode(targetCondition);
        _nodes[fromNodeId].AddBranch(branchCondition, targetNodeId);
        return targetNodeId;
    }

    private int GetOrCreateNode(Condition condition)
    {
        if (_nodeIdByCondition.TryGetValue(condition, out var nodeId))
        {
            return nodeId;
        }

        nodeId = _nodes.Count;
        _nodes.Add(new ConditionTreeNode(condition));
        _nodeIdByCondition.Add(condition, nodeId);
        return nodeId;
    }
}
