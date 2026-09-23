using HotChocolate.Features;
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
    /// Registers a callback that modifies the cost options for the current request.
    /// </summary>
    /// <param name="builder">
    /// The operation request builder.
    /// </param>
    /// <param name="configure">
    /// A delegate that modifies the request's cost options. Values the delegate does not touch
    /// keep the gateway's <see cref="FusionCostOptions"/> value. Calling this more than once
    /// applies every delegate in registration order.
    /// </param>
    /// <returns>
    /// Returns the operation request builder.
    /// </returns>
    public static OperationRequestBuilder ModifyCostOptions(
        this OperationRequestBuilder builder,
        Action<FusionCostOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        GetOrCreateModifiers(builder.Features).Modifiers.Add(configure);
        return builder;
    }

    /// <summary>
    /// Registers a callback that modifies the cost options for the current request.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="configure">
    /// A delegate that modifies the request's cost options. Values the delegate does not touch
    /// keep the gateway's <see cref="FusionCostOptions"/> value. Calling this more than once
    /// applies every delegate in registration order.
    /// </param>
    public static void ModifyCostOptions(this RequestContext context, Action<FusionCostOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configure);

        GetOrCreateModifiers(context.Features).Modifiers.Add(configure);
    }

    /// <summary>
    /// Gets the cost options in effect for the request: a copy of <paramref name="gatewayOptions"/>
    /// with every registered <c>ModifyCostOptions</c> delegate applied, or
    /// <paramref name="gatewayOptions"/> itself when the request registered no delegate.
    /// </summary>
    internal static FusionCostOptions GetEffectiveCostOptions(
        this RequestContext context,
        FusionCostOptions gatewayOptions)
    {
        if (!context.Features.TryGet<FusionCostOptionsModifiers>(out var modifiers))
        {
            return gatewayOptions;
        }

        var options = gatewayOptions.Clone();

        foreach (var configure in modifiers.Modifiers)
        {
            configure(options);
        }

        options.MakeReadOnly();

        return options;
    }

    private static FusionCostOptionsModifiers GetOrCreateModifiers(IFeatureCollection features)
    {
        var modifiers = features.Get<FusionCostOptionsModifiers>();

        if (modifiers is null)
        {
            modifiers = new FusionCostOptionsModifiers();
            features.Set(modifiers);
        }

        return modifiers;
    }
}
