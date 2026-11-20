using System.Buffers;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore
{
    private Dictionary<CompositeResultDocument.Cursor, RequirementFailure>? _requirementErrors;

    /// <summary>
    /// Associates the first source error with an unresolved input field.
    /// </summary>
    internal void RecordRequirementError(CompositeResultElement field, IError error)
    {
        if (field.ValueKind is JsonValueKind.Undefined)
        {
            _requirementErrors ??= [];
            _requirementErrors.TryAdd(field.Cursor, new RequirementFailure(error, IsSkipped: false));
        }
    }

    internal RequirementFailure? GetRequirementFailure(CompositeResultElement field)
        => _requirementErrors is not null && _requirementErrors.TryGetValue(field.Cursor, out var failure)
            ? failure
            : null;

    /// <summary>
    /// Carries failed inputs through lookup dependencies to the result fields they left unresolved.
    /// </summary>
    private void PropagateRequirementErrors(IOperationPlan plan, IVariableValueCollection variables)
    {
        if (_result.Data.IsNullOrInvalidated)
        {
            return;
        }

        foreach (var dependency in RequirementDependency.GetDependencies(plan))
        {
            if (!dependency.IsSkipped(variables))
            {
                PropagateRequirementErrors(dependency);
            }
        }
    }

    private void PropagateRequirementErrors(RequirementDependency dependency)
    {
        var target = dependency.Target;
        var requirements = dependency.Requirements.AsSpan();
        var segmentCount = CountRequirementAnchorSegments(requirements, target.Length);
        var segments = RentRequirementAnchorSegments(requirements, target.Length, segmentCount);

        try
        {
            foreach (var element in CollectTargetElementsUnsafe(target))
            {
                if (element.ValueKind is not JsonValueKind.Object)
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
                        RecordUnresolvedFields(element, dependency.Selections, error);

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
                ? GetRequirementFailure(input)?.Cause
                : null;
        }

        return ResultDataMapper.GetRequirementError(anchor, requirement, _schema, _requirementErrors!);
    }

    private void RecordUnresolvedFields(
        CompositeResultElement element,
        ResultSelectionSet selections,
        IError error)
    {
        if (element.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                RecordUnresolvedFields(item, selections, error);
            }
        }
        else if (element.ValueKind is JsonValueKind.Object && element.SelectionSet is { } selectionSet)
        {
            foreach (var selection in selectionSet.Selections)
            {
                if (!IsSelectionActive(selection)
                    || !selections.TryGetSelection(selection.ResponseName, selectionSet.Type, out var children))
                {
                    continue;
                }

                var field = element.GetPropertyBySelectionId(selection.Id);

                if (field.ValueKind is JsonValueKind.Undefined)
                {
                    _requirementErrors!.TryAdd(field.Cursor, new RequirementFailure(error, IsSkipped: true));
                }
                else if (children is not null)
                {
                    RecordUnresolvedFields(field, children, error);
                }
            }
        }
    }
}

internal readonly record struct RequirementFailure(IError Cause, bool IsSkipped);
