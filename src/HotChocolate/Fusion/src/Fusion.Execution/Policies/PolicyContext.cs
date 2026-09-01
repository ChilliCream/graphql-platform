using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents the mutable context used for policy evaluation.
/// </summary>
internal sealed class PolicyContext : IPolicyContext
{
    private readonly OperationPlanContext? _operationContext;
    private readonly IFeatureCollection? _features;
    private readonly PolicySelection _selection = new();
    private bool _hasSelection;
    private ClaimsPrincipal _user = null!;
    private bool[] _denied = [];
    private string?[] _reasons = [];
    private int _count;

    public PolicyContext(OperationPlanContext operationContext)
    {
        ArgumentNullException.ThrowIfNull(operationContext);
        _operationContext = operationContext;
    }

    public PolicyContext(IFeatureCollection features)
    {
        ArgumentNullException.ThrowIfNull(features);
        _features = features;
    }

    public ClaimsPrincipal User => _user;

    public PolicySelection? Selection => _hasSelection ? _selection : null;

    public IFeatureCollection Features => _features ?? _operationContext!.Features;

    public void Deny(int index, string? reason = null)
    {
        if ((uint)index >= (uint)_count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _denied[index] = true;
        _reasons[index] = reason;
    }

    /// <summary>
    /// Resets this context for a request-constant evaluation with <see cref="Selection"/> set to <c>null</c>.
    /// </summary>
    internal void ResetForRequest(ClaimsPrincipal user)
    {
        _user = user;
        _hasSelection = false;
        EnsureCapacity(1);
        _denied[0] = false;
        _reasons[0] = null;
        _count = 1;
    }

    /// <summary>
    /// Resets this context for an evaluation against a batch of entities.
    /// </summary>
    internal void ResetForResource(
        ClaimsPrincipal user,
        ITypeDefinition type,
        ISelection? selection,
        IVariableValueCollection variables,
        ReadOnlyMemory<CompositeResultElement> entities)
    {
        _user = user;
        _hasSelection = true;
        _selection.Reset(type, selection, variables, entities);
        var count = entities.Length;
        EnsureCapacity(count);
        Array.Clear(_denied, 0, count);
        Array.Clear(_reasons, 0, count);
        _count = count;
    }

    /// <summary>
    /// Clears references to data from the completed evaluation.
    /// </summary>
    internal void Clear()
    {
        _selection.Clear();
        _user = null!;
        _hasSelection = false;
        _count = 0;
    }

    internal bool IsDenied(int index) => _denied[index];

    internal string? GetReason(int index) => _reasons[index];

    internal PolicyDecision GetDecision(int index) => new(_denied[index], _reasons[index]);

    private void EnsureCapacity(int count)
    {
        if (_denied.Length >= count)
        {
            return;
        }

        var size = Math.Max(count, Math.Max(4, _denied.Length * 2));
        _denied = new bool[size];
        _reasons = new string?[size];
    }
}
