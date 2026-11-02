namespace HotChocolate.Fusion.Policies.Audit;

/// <summary>
/// Creates the audit trail of an executor invocation.
/// </summary>
public interface IAuditProvider
{
    /// <summary>
    /// Creates the trail for one executor invocation, which is a single request, a request batch,
    /// or a variable batch. The method runs once per invocation, on the first protected variable
    /// set, and not at all for an invocation without a protected operation.
    /// </summary>
    /// <param name="requestServices">
    /// The request scoped service provider of the invocation.
    /// </param>
    IAuditTrail CreateTrail(IServiceProvider requestServices);
}
