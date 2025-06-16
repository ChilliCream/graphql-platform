namespace HotChocolate.Execution;

/// <summary>
/// Provides extension methods for the maximum execution depth feature.
/// </summary>
public static class MaxExecutionDepthRequestOverridesExtensions
{
    /// <summary>
    /// Skips the request execution depth analysis.
    /// </summary>
    public static OperationRequestBuilder SkipExecutionDepthAnalysis(
        this OperationRequestBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Features.Get<MaxExecutionDepthRequestOverrides>();

        if (options is null)
        {
            options = new MaxExecutionDepthRequestOverrides(SkipValidation: true);
        }
        else
        {
            options = options with { SkipValidation = true };
        }

        builder.Features.Set(options);
        return builder;
    }

    /// <summary>
    /// Set allowed-execution-depth for this request and override the
    /// global allowed execution depth.
    /// </summary>
    public static OperationRequestBuilder SetMaximumAllowedExecutionDepth(
        this OperationRequestBuilder builder,
        int maximumAllowedDepth)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Features.Get<MaxExecutionDepthRequestOverrides>();

        if (options is null)
        {
            options = new MaxExecutionDepthRequestOverrides(MaxAllowedDepth: maximumAllowedDepth);
        }
        else
        {
            options = options with { MaxAllowedDepth = maximumAllowedDepth };
        }

        builder.Features.Set(options);
        return builder;
    }
}
