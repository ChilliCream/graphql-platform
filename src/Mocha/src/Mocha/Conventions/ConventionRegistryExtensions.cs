namespace Mocha;

/// <summary>
/// Provides extension methods on <see cref="IConventionRegistry"/> for applying configuration conventions.
/// </summary>
public static class ConventionRegistryExtensions
{
    /// <summary>
    /// Applies all registered <see cref="IConfigurationConvention{T}"/> conventions to the
    /// specified configuration.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <param name="registry">The convention registry.</param>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The configuration to apply conventions to.</param>
    public static void Configure<T>(
        this IConventionRegistry registry,
        IMessagingConfigurationContext context,
        T configuration)
        where T : MessagingConfiguration
    {
        foreach (var convention in registry.GetConventions<IConfigurationConvention<T>>())
        {
            convention.Configure(context, configuration);
        }
    }

    /// <summary>
    /// Applies all registered <see cref="IConfigurationConvention{T}"/> and
    /// <see cref="IEndpointConfigurationConvention{T}"/> conventions to the specified configuration.
    /// </summary>
    /// <typeparam name="T">The configuration type.</typeparam>
    /// <param name="registry">The convention registry.</param>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The messaging transport that owns the endpoint being configured.</param>
    /// <param name="configuration">The configuration to apply conventions to.</param>
    public static void Configure<T>(
        this IConventionRegistry registry,
        IMessagingConfigurationContext context,
        MessagingTransport transport,
        T configuration)
        where T : MessagingConfiguration
    {
        foreach (var convention in registry.GetConventions<IConfigurationConvention<T>>())
        {
            convention.Configure(context, configuration);
        }

        foreach (var convention in registry.GetConventions<IEndpointConfigurationConvention<T>>())
        {
            convention.Configure(context, transport, configuration);
        }
    }
}
