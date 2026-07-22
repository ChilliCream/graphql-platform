using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// The evaluation context for policies with data requirements, which are evaluated
/// against a batch of entities and deny entities by their position in the batch.
/// </summary>
internal sealed class RequirementPolicyContext : IPolicyContext
{
    private readonly OperationPlanContext _operationContext;
    private readonly bool[] _denied;
    private readonly string?[] _denialReasons;
    private readonly int _entityCount;

    public RequirementPolicyContext(
        OperationPlanContext operationContext,
        ISelection? selection,
        ITypeDefinition type,
        ClaimsPrincipal user,
        PolicyDenialBehavior onDenied,
        bool[] denied,
        string?[] denialReasons,
        int entityCount)
    {
        _operationContext = operationContext;
        Selection = selection;
        Type = type;
        User = user;
        OnDenied = onDenied;
        _denied = denied;
        _denialReasons = denialReasons;
        _entityCount = entityCount;
    }

    public ISelection? Selection { get; }

    public ITypeDefinition Type { get; }

    public PolicyDenialBehavior OnDenied { get; }

    public ClaimsPrincipal User { get; }

    public IFeatureCollection Features => _operationContext.Features;

    public void Deny(int index, string? reason = null)
    {
        if ((uint)index >= (uint)_entityCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _denied[index] = true;
        _denialReasons[index] = reason;
    }
}
