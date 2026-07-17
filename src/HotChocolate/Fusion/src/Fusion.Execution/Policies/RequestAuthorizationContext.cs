using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

internal sealed class RequestAuthorizationContext : IAuthorizationContext
{
    private readonly OperationPlanContext _operationContext;
    private readonly ISelection? _selection;
    private readonly ITypeDefinition _type;
    private readonly ClaimsPrincipal _user;
    private readonly PolicyApplication _application;
    private string? _reason;
    private int _denied;
    private int _active = 1;

    public RequestAuthorizationContext(
        OperationPlanContext operationContext,
        ISelection? selection,
        ITypeDefinition type,
        ClaimsPrincipal user,
        PolicyApplication application)
    {
        _operationContext = operationContext;
        _selection = selection;
        _type = type;
        _user = user;
        _application = application;
    }

    public ISelection? Selection
    {
        get
        {
            EnsureActive();
            return _selection;
        }
    }

    public ITypeDefinition Type
    {
        get
        {
            EnsureActive();
            return _type;
        }
    }

    public PolicyDenialBehavior OnDenied
    {
        get
        {
            EnsureActive();
            return _application.OnDenied;
        }
    }

    public ClaimsPrincipal User
    {
        get
        {
            EnsureActive();
            return _user;
        }
    }

    public IFeatureCollection Features
    {
        get
        {
            EnsureActive();
            return _operationContext.Features;
        }
    }

    internal AuthorizationPolicyDecision Decision
        => new(Volatile.Read(ref _denied) == 1, _reason);

    public void Deny(int index, string? reason = null)
    {
        EnsureActive();

        ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);

        _reason = reason;
        Volatile.Write(ref _denied, 1);
    }

    internal void Deactivate() => Volatile.Write(ref _active, 0);

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _active) == 0, this);
    }
}

internal readonly record struct AuthorizationPolicyDecision(bool IsDenied, string? Reason);
