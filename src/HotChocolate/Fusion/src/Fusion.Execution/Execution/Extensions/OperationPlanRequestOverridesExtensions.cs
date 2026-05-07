using HotChocolate.Features;
using HotChocolate.Fusion.Execution;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HotChocolate.Execution;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides extension methods to configure per-request operation plan overrides.
/// </summary>
public static class OperationPlanRequestOverridesExtensions
{
    /// <summary>
    /// Allows the current request to retrieve the operation plan when the
    /// <c>Fusion-Operation-Plan</c> header is set, regardless of the schema-level
    /// <see cref="FusionRequestOptions.AllowOperationPlanRequests"/> setting.
    /// </summary>
    /// <param name="builder">
    /// The operation request builder.
    /// </param>
    /// <returns>
    /// Returns the operation request builder.
    /// </returns>
    public static OperationRequestBuilder AllowOperationPlanRequest(
        this OperationRequestBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Features.Get<OperationPlanRequestOverrides>();

        if (options is null)
        {
            options = new OperationPlanRequestOverrides(IsAllowed: true);
        }
        else
        {
            options = options with { IsAllowed = true };
        }

        builder.Features.Set(options);
        return builder;
    }
}
