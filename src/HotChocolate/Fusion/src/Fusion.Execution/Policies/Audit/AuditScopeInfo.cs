namespace HotChocolate.Fusion.Policies.Audit;

/// <summary>
/// The identity of the variable set an <see cref="IAuditScope"/> records.
/// </summary>
/// <param name="OperationId">
/// The id of the operation.
/// </param>
/// <param name="PlanId">
/// The id of the operation plan.
/// </param>
/// <param name="RequestIndex">
/// The index of the request within its executor invocation, or <c>-1</c> if the invocation is a
/// single request.
/// </param>
/// <param name="VariableSetIndex">
/// The index of the variable set within the request.
/// </param>
public readonly record struct AuditScopeInfo(
    string OperationId,
    string PlanId,
    int RequestIndex,
    int VariableSetIndex);
