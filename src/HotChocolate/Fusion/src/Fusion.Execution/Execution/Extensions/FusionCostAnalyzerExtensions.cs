using HotChocolate.Fusion.Execution;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace HotChocolate.Execution;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Cost analyzer extensions for <see cref="OperationRequestBuilder"/> and <see cref="RequestContext"/>.
/// </summary>
public static class FusionCostAnalyzerExtensions
{
    /// <summary>
    /// Sets the cost options for the current request.
    /// </summary>
    /// <param name="builder">
    /// The operation request builder.
    /// </param>
    /// <param name="options">
    /// The cost options.
    /// </param>
    /// <returns>
    /// Returns the operation request builder.
    /// </returns>
    public static OperationRequestBuilder SetCostOptions(
        this OperationRequestBuilder builder,
        FusionRequestCostOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.Features.Set(options);
        return builder;
    }

    /// <summary>
    /// Sets the cost options for the current request.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="options">
    /// The cost options.
    /// </param>
    public static void SetCostOptions(this RequestContext context, FusionRequestCostOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        context.Features.Set(options);
    }

    internal static FusionRequestCostOptions? TryGetCostOptions(this RequestContext context)
    {
        return context.Features.TryGet<FusionRequestCostOptions>(out var options) ? options : null;
    }
}
