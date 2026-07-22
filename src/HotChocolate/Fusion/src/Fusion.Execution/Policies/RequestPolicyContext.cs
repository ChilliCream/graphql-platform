using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// The evaluation context for policies without data requirements, which produce a
/// single decision per request.
/// </summary>
internal sealed class RequestPolicyContext : IPolicyContext
{
    private readonly OperationPlanContext _operationContext;
    private string? _reason;
    private bool _denied;

    public RequestPolicyContext(
        OperationPlanContext operationContext,
        ISelection? selection,
        ITypeDefinition type,
        ClaimsPrincipal user,
        PolicyDenialBehavior onDenied)
    {
        _operationContext = operationContext;
        Selection = selection;
        Type = type;
        User = user;
        OnDenied = onDenied;
    }

    public ISelection? Selection { get; }

    public ITypeDefinition Type { get; }

    public PolicyDenialBehavior OnDenied { get; }

    public ClaimsPrincipal User { get; }

    public IFeatureCollection Features => _operationContext.Features;

    internal PolicyDecision Decision
        => new(_denied, _reason);

    public void Deny(int index, string? reason = null)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);

        _reason = reason;
        _denied = true;
    }
}
