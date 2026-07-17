using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

internal sealed class AuthorizationContext : IAuthorizationContext
{
    private readonly OperationPlanContext _operationContext;
    private readonly ISelection? _selection;
    private readonly ITypeDefinition _type;
    private readonly ClaimsPrincipal _user;
    private readonly PolicyDenialBehavior _onDenied;
    private readonly bool[] _denied;
    private readonly string?[] _denialReasons;
    private readonly int _entityCount;
    private int _active = 1;

    public AuthorizationContext(
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
        _selection = selection;
        _type = type;
        _user = user;
        _onDenied = onDenied;
        _denied = denied;
        _denialReasons = denialReasons;
        _entityCount = entityCount;
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
            return _onDenied;
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

    public void Deny(int index, string? reason = null)
    {
        EnsureActive();

        if ((uint)index >= (uint)_entityCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _denied[index] = true;
        _denialReasons[index] = reason;
    }

    internal void Deactivate() => Volatile.Write(ref _active, 0);

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _active) == 0, this);
    }
}
