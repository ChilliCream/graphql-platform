using System.Security.Claims;

namespace HotChocolate.Fusion.Policies.Audit;

internal sealed class NoOpAuditTrail : IAuditTrail
{
    public static NoOpAuditTrail Instance { get; } = new();

    private NoOpAuditTrail()
    {
    }

    public string InvocationId => string.Empty;

    public IAuditScope BeginRequest(AuditScopeInfo info, ClaimsPrincipal user) => NoOpAuditScope.Instance;
}
