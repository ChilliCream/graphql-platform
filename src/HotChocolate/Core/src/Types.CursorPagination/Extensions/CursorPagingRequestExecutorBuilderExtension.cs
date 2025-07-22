using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// GraphQL Configurations for cursor pagination.
/// </summary>
public static class CursorPagingRequestExecutorBuilderExtension
{
    /// <summary>
    /// Adds the queryable cursor paging provider to the DI.
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
    /// <param name="inlineTotalCount">
    /// Specifies that the paging provider shall inline the total count query into the
    /// sliced query in order to have a single database call.
    /// Some database providers might not support this feature.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddQueryableCursorPagingProvider(
        this IRequestExecutorBuilder builder,
        string? providerName = null,
        bool defaultProvider = false,
        bool? inlineTotalCount = null)
        => AddCursorPagingProvider(
            builder,
            _ => new QueryableCursorPagingProvider(inlineTotalCount),
            providerName,
            defaultProvider);

    /// <summary>
    /// Adds a cursor paging provider to the DI.
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
    public static IRequestExecutorBuilder AddCursorPagingProvider<TProvider>(
        this IRequestExecutorBuilder builder,
        string? providerName = null,
        bool defaultProvider = false)
        where TProvider : CursorPagingProvider
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<TProvider>();

        AddCursorPagingProvider(
            builder,
            s => s.GetRequiredService<TProvider>(),
            providerName,
            defaultProvider);

        return builder;
    }

    /// <summary>
    /// Adds a cursor paging provider to the DI.
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
    public static IRequestExecutorBuilder AddCursorPagingProvider<TProvider>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, TProvider> factory,
        string? providerName = null,
        bool defaultProvider = false)
        where TProvider : CursorPagingProvider
    {
        ArgumentNullException.ThrowIfNull(builder);

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
