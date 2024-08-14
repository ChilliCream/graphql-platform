using HotChocolate.Data;
using HotChocolate.Data.Pagination;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// This class provides configuration helper methods for the entity framework integration.
/// </summary>
public static class EntityFrameworkRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Registers a DbContext as a pooled DbContext service.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <typeparam name="T">
    /// The type of the DbContext.
    /// </typeparam>
    /// <returns>
    /// Returns the request executor builder.
    /// </returns>
    public static IRequestExecutorBuilder RegisterDbContextFactory<T>(
        this IRequestExecutorBuilder builder)
        where T : DbContext
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder, ContextFactoryParameterExpressionBuilder<T>>();
        return builder;
    }

    /// <summary>
    /// Adds a cursor paging provider that uses native keyset pagination.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="providerName">
    /// The name of the provider.
    /// </param>
    /// <param name="defaultProvider">
    /// Defines if this provider is the default provider.
    /// </param>
    /// <returns>
    /// Returns the request executor builder.
    /// </returns>
    public static IRequestExecutorBuilder AddDbContextCursorPagingProvider(
        this IRequestExecutorBuilder builder,
        string? providerName = null,
        bool defaultProvider = false)
        => builder.AddCursorPagingProvider<EfQueryableCursorPagingProvider>(providerName, defaultProvider);
}
