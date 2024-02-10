using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides global configuration methods for mutation conventions to the
/// <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class QueryRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Enables mutation conventions which will simplify creating GraphQL mutations.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder
    /// </param>
    /// <returns>
    /// The request executor builder
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is null.
    /// </exception>
    public static IRequestExecutorBuilder AddQueryConventions(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder
            .ConfigureSchema(
                c =>
                {
                    c.ContextData[ErrorContextDataKeys.ErrorConventionEnabled] = true;
                })
            .AddFieldResultTypeDiscovery()
            .TryAddTypeInterceptor<QueryConventionTypeInterceptor>();

        return builder;
    }
}
