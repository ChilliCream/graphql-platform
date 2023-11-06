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
                ApplyToAllMutations = applyToAllMutations
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
            .ConfigureSchema(c => c.ContextData[MutationContextDataKeys.Options] = options)
            .TryAddTypeInterceptor<MutationConventionTypeInterceptor>()
            .AddTypeDiscoveryHandler(c => new MutationResultTypeDiscoveryHandler(c.TypeInspector))
            .ConfigureSchema(c => c.TryAddSchemaDirective(new MutationDirective()));

        return builder;
    }

    /// <summary>
    /// Defines the common interface that all errors implement.
    /// To specify the interface you can either provide a interface runtime type or a HotChocolate
    /// interface schema type.
    ///
    /// This has to be used together with <see cref="ErrorAttribute"/>  or
    /// <see cref="MutationObjectFieldDescriptorExtensions.Error"/>
    /// </summary>
    /// <param name="builder">
    /// The request executor builder
    /// </param>
    /// <typeparam name="T">
    /// The type that is used as the common interface
    /// </typeparam>
    /// <returns>
    /// The schema builder
    /// </returns>
    public static IRequestExecutorBuilder AddErrorInterfaceType<T>(
        this IRequestExecutorBuilder builder) =>
        builder.ConfigureSchema(x => x.AddErrorInterfaceType<T>());

    /// <summary>
    /// Defines the common interface that all errors implement.
    /// To specify the interface you can either provide a interface runtime type or a HotChocolate
    /// interface schema type.
    ///
    /// This has to be used together with <see cref="ErrorAttribute"/>  or
    /// <see cref="MutationObjectFieldDescriptorExtensions.Error"/>
    /// </summary>
    /// <param name="builder">
    /// The request executor builder
    /// </param>
    /// <param name="type">
    /// The type that is used as the common interface
    /// </param>
    /// <returns>
    /// The request executor builder
    /// </returns>
    public static IRequestExecutorBuilder AddErrorInterfaceType(
        this IRequestExecutorBuilder builder,
        Type type) =>
        builder.ConfigureSchema(x => x.AddErrorInterfaceType(type));
    
    /// <summary>
    /// Adds a new error registrar.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder
    /// </param>
    /// <typeparam name="T">
    /// The mutation error registrar.
    /// </typeparam>
    /// <returns>
    /// The request executor builder
    /// </returns>
    public static IRequestExecutorBuilder AddMutationErrorConfiguration<T>(this IRequestExecutorBuilder builder) 
        where T : MutationErrorConfiguration, new()
        => builder.TryAddTypeInterceptor(new MutationErrorTypeInterceptor<T>(new T()));

    /// <summary>
    /// Adds a new error registrar.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder
    /// </param>
    /// <param name="configuration">
    /// The mutation error configuration.
    /// </param>
    /// <typeparam name="T">
    /// The mutation error registrar.
    /// </typeparam>
    /// <returns>
    /// The request executor builder
    /// </returns>
    public static IRequestExecutorBuilder AddMutationErrorConfiguration<T>(
        this IRequestExecutorBuilder builder,
        T configuration) 
        where T : MutationErrorConfiguration
        => builder.TryAddTypeInterceptor(new MutationErrorTypeInterceptor<T>(configuration));
}