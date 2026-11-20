using System.Buffers;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore
{
    private Dictionary<int, IError>? _requirementErrors;

    internal void RecordRequirementError(CompositeResultElement field, IError error)
    {
        if (field.ValueKind is JsonValueKind.Undefined)
        {
            _requirementErrors ??= [];
            _requirementErrors.TryAdd(field.Cursor.Value, error);
        }
    }

    /// <summary>
    /// Reports a failed requirement at each selected field that its lookup left unresolved.
    /// </summary>
    internal void FinalizeRequirementErrors(IOperationPlan plan, IVariableValueCollection variables)
    {
        if (_requirementErrors is null || _result.Data.IsNullOrInvalidated)
        {
            return;
        }

        var visited = new HashSet<IOperationPlanNode>(ReferenceEqualityComparer.Instance);
        lock (_lock)
        {
            foreach (var node in plan.AllNodes)
            {
                Visit(node);
            }

            var reportedPaths = _errors?.Select(static e => e.Path).ToHashSet();
            foreach (var (cursor, error) in _requirementErrors)
            {
                var field = new CompositeResultElement(_result, new CompositeResultDocument.Cursor(cursor));
                var selection = field.AssertSelection();
                if (field.ValueKind is not JsonValueKind.Undefined || !IsClientSelection(selection))
                {
                    continue;
                }

                var path = field.Path;
                if (reportedPaths?.Contains(path) == true
                    || !TryGetResult(path, out var reachable)
                    || reachable.Cursor != field.Cursor)
                {
                    continue;
                }

                if (!_valueCompletion.ApplyFieldError(field, selection, error, path))
                {
                    _result.Data.Invalidate();
                    break;
                }
            }
        }

        // A lookup can be skipped for every target. Its consumers still inherit the
        // error through the fields that lookup would have supplied.
        void Visit(IOperationPlanNode node)
        {
            if (!visited.Add(node))
            {
                return;
            }

            foreach (var dependency in node.Dependencies)
            {
                Visit(dependency);
            }

            foreach (var dependency in node.OptionalDependencies)
            {
                Visit(dependency);
            }

            if (node is ExecutionNode step
                    && ExecutionNode.IsSkipped(variables, step.Conditions, step.ConditionAlternatives)
                || node is OperationDefinition definition
                    && ExecutionNode.IsSkipped(variables, definition.Conditions, definition.ConditionAlternatives))
            {
                return;
            }

            switch (node)
            {
                case OperationExecutionNode op:
                    PropagateRequirementErrors(op.Target, op.Requirements, op.ResultSelectionSet);
                    break;
                case ApolloOperationExecutionNode op:
                    PropagateRequirementErrors(op.Target, op.Requirements, op.ResultSelectionSet);
                    break;
                case OperationBatchExecutionNode batch:
                    foreach (var operation in batch.Operations)
                    {
                        Visit(operation);
                    }
                    break;
                case ApolloOperationBatchExecutionNode batch:
                    foreach (var operation in batch.Operations)
                    {
                        Visit(operation);
                    }
                    break;
                case SingleOperationDefinition op:
                    PropagateRequirementErrors(op.Target, op.Requirements, op.ResultSelectionSet);
                    break;
                case BatchOperationDefinition op:
                    foreach (var target in op.Targets)
                    {
                        PropagateRequirementErrors(target, op.Requirements, op.ResultSelectionSet);
                    }
                    break;
            }
        }
    }

    private bool IsClientSelection(Selection selection)
    {
        for (var current = selection;
            current is not null;
            current = current.DeclaringSelectionSet.DeclaringSelection)
        {
            if (current.IsInternal
                || !current.IsIncluded(_includeFlags, _wideIncludeFlags)
                || current.IsDeferred(_deferFlags, _wideDeferFlags))
            {
                return false;
            }
        }

        return true;
    }

    private void PropagateRequirementErrors(
        SelectionPath target,
        ReadOnlySpan<OperationRequirement> requirements,
        ResultSelectionSet selections)
    {
        if (requirements.IsEmpty)
        {
            return;
        }

        var segmentCount = CountRequirementAnchorSegments(requirements, target.Length);
        var segments = RentRequirementAnchorSegments(requirements, target.Length, segmentCount);
        var unresolved = new List<CompositeResultElement>();

        try
        {
            foreach (var element in CollectTargetElementsUnsafe(target))
            {
                if (element.ValueKind is not JsonValueKind.Object)
                {
                    continue;
                }

                unresolved.Clear();
                CollectUnresolvedFields(element, selections, unresolved);
                if (unresolved.Count == 0)
                {
                    continue;
                }

                var offset = 0;
                foreach (var requirement in requirements)
                {
                    var length = Math.Max(requirement.Path.Length - target.Length, 0);
                    var anchorSegments = segments.AsSpan(offset, length);
                    offset += length;
                    if (!TryGetRequirementAnchor(element, anchorSegments, out var anchor))
                    {
                        continue;
                    }

                    if (GetRequirementError(anchor, requirement) is { } error)
                    {
                        foreach (var field in unresolved)
                        {
                            RecordRequirementError(field, error);
                        }

                        break;
                    }
                }
            }
        }
        finally
        {
            if (segmentCount > 0)
            {
                ArrayPool<RequirementAnchorSegment>.Shared.Return(segments, clearArray: true);
            }
        }
    }

    private IError? GetRequirementError(CompositeResultElement anchor, OperationRequirement requirement)
    {
        if (TryGetSimpleRequirementFieldName(requirement, out var name))
        {
            return anchor.TryGetProperty(name, out var input) && input.ValueKind is JsonValueKind.Undefined
                ? _requirementErrors!.GetValueOrDefault(input.Cursor.Value)
                : null;
        }

        var buffer = GetVariableWriter();
        var start = buffer.Position;
        _jsonWriter.Reset(buffer);

        try
        {
            return ResultDataMapper.GetRequirementError(anchor, requirement, _schema, _jsonWriter, _requirementErrors!);
        }
        finally
        {
            buffer.ResetTo(start);
        }
    }

    private static void CollectUnresolvedFields(
        CompositeResultElement element,
        ResultSelectionSet selections,
        List<CompositeResultElement> unresolved)
    {
        if (element.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectUnresolvedFields(item, selections, unresolved);
            }
        }
        else if (element.ValueKind is JsonValueKind.Object && element.SelectionSet is { } selectionSet)
        {
            foreach (var selection in selections.Selections)
            {
                if (element.TryGetProperty(selection.ResponseName, out var field))
                {
                    if (field.ValueKind is JsonValueKind.Undefined)
                    {
                        unresolved.Add(field);
                    }
                    else if (selection.Child is { } child)
                    {
                        CollectUnresolvedFields(field, child, unresolved);
                    }
                }
            }

            foreach (var fragment in selections.Fragments)
            {
                if (fragment.TypeCondition?.IsAssignableFrom(selectionSet.Type) != false)
                {
                    CollectUnresolvedFields(element, fragment.Body, unresolved);
                }
            }
        }
    }
}
