using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// GraphQL Configurations for offset pagination.
/// </summary>
public static class OffsetPagingRequestExecutorBuilderExtension
{
    /// <summary>
    /// Adds the queryable offset paging provider to the DI.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="providerName">
    /// The name the provider shall have.
    /// </param>
    /// <param name="defaultProvider">
    /// Defines if the registered provider shall be registered as the default provider.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddQueryableOffsetPagingProvider(
        this IRequestExecutorBuilder builder,
        string? providerName = null,
        bool defaultProvider = false)
        => AddOffsetPagingProvider<QueryableOffsetPagingProvider>(
            builder,
            providerName,
            defaultProvider);

    /// <summary>
    /// Adds a offset paging provider to the DI.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="providerName">
    /// The name the provider shall have.
    /// </param>
    /// <param name="defaultProvider">
    /// Defines if the registered provider shall be registered as the default provider.
    /// </param>
    /// <typeparam name="TProvider">
    /// The type of the provider.
    /// </typeparam>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddOffsetPagingProvider<TProvider>(
        this IRequestExecutorBuilder builder,
        string? providerName = null,
        bool defaultProvider = false)
        where TProvider : OffsetPagingProvider
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.TryAddSingleton<TProvider>();

        AddOffsetPagingProvider(
            builder,
            s => s.GetRequiredService<TProvider>(),
            providerName,
            defaultProvider);

        return builder;
    }

    /// <summary>
    /// Adds a offset paging provider to the DI.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="factory">
    /// A factory to create the paging provider.
    /// </param>
    /// <param name="providerName">
    /// The name the provider shall have.
    /// </param>
    /// <param name="defaultProvider">
    /// Defines if the registered provider shall be registered as the default provider.
    /// </param>
    /// <typeparam name="TProvider">
    /// The type of the provider.
    /// </typeparam>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddOffsetPagingProvider<TProvider>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, TProvider> factory,
        string? providerName = null,
        bool defaultProvider = false)
        where TProvider : OffsetPagingProvider
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (defaultProvider)
        {
            var service = ServiceDescriptor.Singleton(
                typeof(PagingProviderEntry),
                CreateEntry);
            builder.Services.Insert(0, service);
        }
        else
        {
            builder.Services.AddSingleton(CreateEntry);
        }

        return builder;

        PagingProviderEntry CreateEntry(IServiceProvider services)
            => new(providerName, factory(services));
    }
}
