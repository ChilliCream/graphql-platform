namespace HotChocolate.Execution;

/// <summary>
/// Represents the request-specific overrides for the maximum execution depth validation.
/// </summary>
/// <param name="SkipValidation">
/// If <c>true</c>, the maximum execution depth validation will be skipped for this request.
/// </param>
/// <param name="MaxAllowedDepth">
/// The maximum allowed execution depth for this request.
/// </param>
public sealed record MaxExecutionDepthRequestOverrides(
    bool SkipValidation = false,
    int MaxAllowedDepth = 0);
