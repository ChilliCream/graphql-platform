using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides global configuration methods for mutation conventions to the
/// <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class MutationRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Enables mutation conventions which will simplify creating GraphQL mutations.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder
    /// </param>
    /// <param name="applyToAllMutations">
    /// Defines if the mutation convention defaults shall be applied to all mutations.
    /// </param>
    /// <returns>
    /// The request executor builder
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is null.
    /// </exception>
    public static IRequestExecutorBuilder AddMutationConventions(
        this IRequestExecutorBuilder builder,
        bool applyToAllMutations = true)
        => AddMutationConventions(
            builder,
            new MutationConventionOptions
            {
                ApplyToAllMutations = applyToAllMutations,
            });

    /// <summary>
    /// Enables mutation conventions which will simplify creating GraphQL mutations.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder
    /// </param>
    /// <param name="options">
    /// The mutation convention options.
    /// </param>
    /// <returns>
    /// The request executor builder
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is null.
    /// </exception>
    public static IRequestExecutorBuilder AddMutationConventions(
        this IRequestExecutorBuilder builder,
        MutationConventionOptions options)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder
            .ConfigureSchema(c =>
            {
                c.ContextData[MutationContextDataKeys.Options] = options;
                c.ContextData[ErrorContextDataKeys.ErrorConventionEnabled] = true;
            })
            .AddFieldResultTypeDiscovery()
            .TryAddTypeInterceptor<MutationConventionTypeInterceptor>()
            .ConfigureSchema(c => c.TryAddSchemaDirective(new MutationDirective()));

        return builder;
    }
}
