using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Builds one <see cref="ConditionTree"/> from a boundary's raw selections,
/// hash-consing nodes on their canonical <see cref="Condition"/> so
/// different fragment spellings that reach the same condition share a node
/// and are grafted onto the tree with the fewest possible edges.
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
    private Condition _rootCondition;

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
        _rootCondition = rootCondition;
        var rootNodeId = GetOrCreateNode(rootCondition);
        InsertSelections(rootCondition, [], selections);
        return new ConditionTree(rootNodeId, _nodes);
    }

    private void InsertSelections(
        Condition condition,
        List<PathStep> sourcePath,
        IReadOnlyList<ISelectionNode> selections)
    {
        foreach (var selection in selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    InsertFieldSelection(field, condition, sourcePath);
                    break;

                case InlineFragmentNode inlineFragment:
                    InsertFragment(
                        inlineFragment.TypeCondition?.Name.Value,
                        inlineFragment.Directives,
                        inlineFragment.SelectionSet.Selections,
                        condition,
                        sourcePath);
                    break;

                case FragmentSpreadNode spread:
                    InsertSpread(spread, condition, sourcePath);
                    break;
            }
        }
    }

    private void InsertSpread(FragmentSpreadNode spread, Condition condition, List<PathStep> sourcePath)
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
            condition,
            sourcePath);

        _fragmentsInProgress.Remove(fragmentName);
    }

    private void InsertFragment(
        string? typeConditionName,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<ISelectionNode> selections,
        Condition condition,
        List<PathStep> sourcePath)
    {
        var branches = new List<BranchCondition>();

        if (typeConditionName is not null)
        {
            branches.Add(BranchCondition.Type(typeConditionName));
        }

        if (!TryAddDirectiveBranches(directives, branches))
        {
            return;
        }

        if (!TryPathForBranches(condition, branches, out var nextPath))
        {
            return;
        }

        var nextCondition = nextPath.Count > 0 ? nextPath[^1].Result : condition;
        var fullPath = Concat(sourcePath, nextPath);
        InsertSelections(nextCondition, fullPath, selections);
    }

    private void InsertFieldSelection(FieldNode field, Condition condition, List<PathStep> sourcePath)
    {
        var branches = new List<BranchCondition>();

        if (!TryAddDirectiveBranches(field.Directives, branches))
        {
            return;
        }

        if (!TryPathForBranches(condition, branches, out var nextPath))
        {
            return;
        }

        var target = nextPath.Count > 0 ? nextPath[^1].Result : condition;
        var fullPath = Concat(sourcePath, nextPath);
        GraftField(fullPath, target, field);
    }

    /// <summary>
    /// Collects one <see cref="BranchCondition"/> per feasible <c>@include</c>/<c>@skip</c>
    /// directive, in source order. Returns <see langword="false"/> when a directive makes the
    /// selection infeasible.
    /// </summary>
    private bool TryAddDirectiveBranches(IReadOnlyList<DirectiveNode> directives, List<BranchCondition> branches)
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

            branches.Add(BranchCondition.Boolean(literal));
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

    /// <summary>
    /// Inserts <paramref name="field"/> at the node for <paramref name="target"/>, grafting the
    /// fewest missing edges from the deepest already-existing node along <paramref name="sourcePath"/>.
    /// </summary>
    private void GraftField(List<PathStep> sourcePath, Condition target, FieldNode field)
    {
        if (_nodeIdByCondition.TryGetValue(target, out var existingId))
        {
            _nodes[existingId].AddField(field);
            return;
        }

        var (_, sourceCondition, sourceLength) = DeepestExistingPrefix(_rootCondition, sourcePath);
        var remainingPath = sourcePath.GetRange(sourceLength, sourcePath.Count - sourceLength);
        var remaining = new List<BranchCondition>(remainingPath.Count);

        foreach (var step in remainingPath)
        {
            remaining.Add(step.Branch);
        }

        var shrunk = ShrinkBranches(sourceCondition, target, remaining);
        List<PathStep> retained;

        if (BranchesEqual(shrunk, remaining))
        {
            retained = remainingPath;
        }
        else if (TryPathForBranches(sourceCondition, shrunk, out var shrunkPath)
            && shrunkPath.Count > 0
            && shrunkPath[^1].Result.Equals(target))
        {
            retained = shrunkPath;
        }
        else
        {
            retained = remainingPath;
        }

        var (parentId, _, retainedLength) = DeepestExistingPrefix(sourceCondition, retained);
        var missing = ErasePathCycles(retained.GetRange(retainedLength, retained.Count - retainedLength));

        if (missing.Count == 0)
        {
            _nodes[parentId].AddField(field);
            return;
        }

        foreach (var step in missing)
        {
            var childId = GetOrCreateNode(step.Result);
            _nodes[parentId].AddBranch(step.Branch, childId);
            parentId = childId;
        }

        _nodes[parentId].AddField(field);
    }

    /// <summary>
    /// Finds the rightmost point along <paramref name="path"/> (starting from
    /// <paramref name="start"/>, which must already have a node) whose condition already has a
    /// node, and returns that node, its condition and how many path steps precede it.
    /// </summary>
    private (int NodeId, Condition Condition, int Length) DeepestExistingPrefix(
        Condition start,
        List<PathStep> path)
    {
        var bestId = _nodeIdByCondition[start];
        var bestCondition = start;
        var bestLength = 0;

        for (var i = 0; i < path.Count; i++)
        {
            if (_nodeIdByCondition.TryGetValue(path[i].Result, out var id))
            {
                bestId = id;
                bestCondition = path[i].Result;
                bestLength = i + 1;
            }
        }

        return (bestId, bestCondition, bestLength);
    }

    /// <summary>
    /// Drops any branch in <paramref name="source"/> whose removal still reaches
    /// <paramref name="target"/> from <paramref name="start"/>, then, when the retained branches
    /// still narrow by type and <paramref name="target"/>'s possible types are a single object,
    /// tries replacing them with the single edge for that object.
    /// </summary>
    private List<BranchCondition> ShrinkBranches(Condition start, Condition target, List<BranchCondition> source)
    {
        var retained = new List<BranchCondition>();

        for (var i = 0; i < source.Count; i++)
        {
            var candidate = new List<BranchCondition>(retained.Count + source.Count - i - 1);
            candidate.AddRange(retained);

            for (var j = i + 1; j < source.Count; j++)
            {
                candidate.Add(source[j]);
            }

            if (!TryConditionForBranches(start, candidate, out var reached) || !reached.Equals(target))
            {
                retained.Add(source[i]);
            }
        }

        if (target.PossibleTypes.Count == 1 && ContainsTypeBranch(retained))
        {
            var objectName = _snapshot.GetSingletonObjectTypeName(target.PossibleTypes);
            var singleton = new List<BranchCondition> { BranchCondition.Type(objectName) };

            foreach (var branch in retained)
            {
                if (branch.TypeName is null)
                {
                    singleton.Add(branch);
                }
            }

            if (TryConditionForBranches(start, singleton, out var singletonReached) && singletonReached.Equals(target))
            {
                return singleton;
            }
        }

        return retained;
    }

    private static bool ContainsTypeBranch(List<BranchCondition> branches)
    {
        foreach (var branch in branches)
        {
            if (branch.TypeName is not null)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryPathForBranches(Condition start, List<BranchCondition> branches, out List<PathStep> path)
    {
        path = new List<PathStep>(branches.Count);
        var condition = start;

        foreach (var branch in branches)
        {
            if (!TryConditionForBranch(condition, branch, out condition))
            {
                path = [];
                return false;
            }

            path.Add(new PathStep(branch, condition));
        }

        return true;
    }

    private bool TryConditionForBranches(Condition start, List<BranchCondition> branches, out Condition result)
    {
        result = start;

        foreach (var branch in branches)
        {
            if (!TryConditionForBranch(result, branch, out result))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryConditionForBranch(Condition start, BranchCondition branch, out Condition result)
    {
        if (branch.TypeName is { } typeName)
        {
            var narrowed = Intersect(start.PossibleTypes, typeName);

            if (narrowed.Count == 0)
            {
                result = default;
                return false;
            }

            result = new Condition(narrowed, start.BooleanCondition);
            return true;
        }

        var literal = branch.Literal!.Value;

        if (!Condition.TryInsertLiteral(start.BooleanCondition, literal, out var nextBooleanCondition))
        {
            result = default;
            return false;
        }

        result = new Condition(start.PossibleTypes, nextBooleanCondition);
        return true;
    }

    private static List<PathStep> ErasePathCycles(List<PathStep> path)
    {
        var kept = new List<PathStep>();

        foreach (var step in path)
        {
            var cycleStart = kept.FindIndex(k => k.Result.Equals(step.Result));

            if (cycleStart >= 0)
            {
                kept.RemoveRange(cycleStart + 1, kept.Count - (cycleStart + 1));
            }
            else
            {
                kept.Add(step);
            }
        }

        return kept;
    }

    private static bool BranchesEqual(List<BranchCondition> left, List<BranchCondition> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!left[i].Equals(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static List<PathStep> Concat(List<PathStep> left, List<PathStep> right)
    {
        var result = new List<PathStep>(left.Count + right.Count);
        result.AddRange(left);
        result.AddRange(right);
        return result;
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

    /// <summary>
    /// One step of a source-level path: the branch condition taken, and the cumulative
    /// <see cref="Condition"/> it reaches.
    /// </summary>
    private readonly record struct PathStep(BranchCondition Branch, Condition Result);
}
