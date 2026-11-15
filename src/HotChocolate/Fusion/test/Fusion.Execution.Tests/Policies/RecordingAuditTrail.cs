using System.Security.Claims;
using HotChocolate.Fusion.Policies.Auditing;

namespace HotChocolate.Fusion.Policies;

internal sealed class RecordingAuditTrail(
    int index,
    RecordingAuditProvider provider,
    Func<int> countSourceSchemaRequests)
    : IAuditTrail
{
    public string InvocationId { get; } = "invocation-" + index;

    public IAuditScope BeginRequest(AuditScopeInfo info, ClaimsPrincipal user)
    {
        var scope = new RecordingAuditScope(
            this,
            info,
            user,
            countSourceSchemaRequests,
            provider.CommitFailure);

        provider.AddScope(scope);

        return provider.RecordFailure is not null || provider.FailFailure is not null
            ? new FaultingAuditScope(scope, provider.RecordFailure, provider.FailFailure)
            : scope;
    }
}
