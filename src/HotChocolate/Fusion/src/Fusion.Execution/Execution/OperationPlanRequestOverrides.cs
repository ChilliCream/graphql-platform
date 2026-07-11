namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents per-request overrides for the operation plan request behavior.
/// </summary>
/// <param name="IsAllowed">
/// A value indicating whether the current request is allowed to retrieve the operation plan
/// when the corresponding header is set, regardless of the schema-level
/// <see cref="FusionRequestOptions.AllowOperationPlanRequests"/> setting.
/// </param>
internal sealed record OperationPlanRequestOverrides(bool IsAllowed = true);
