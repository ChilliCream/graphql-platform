using System.Security.Claims;

namespace HotChocolate.Fusion.Policies.Audit;

/// <summary>
/// The audit trail of one executor invocation, which opens one scope per variable set.
/// </summary>
public interface IAuditTrail
{
    /// <summary>
    /// Gets the identity of the executor invocation, which is empty when the provider assigns none.
    /// </summary>
    string InvocationId { get; }

    /// <summary>
    /// Opens the scope that records the policy decisions of one variable set. The method must be
    /// safe for concurrent calls.
    /// </summary>
    /// <param name="info">
    /// The identity of the variable set.
    /// </param>
    /// <param name="user">
    /// The principal of the request. A recording scope captures it as values.
    /// </param>
    IAuditScope BeginRequest(AuditScopeInfo info, ClaimsPrincipal user);
}
