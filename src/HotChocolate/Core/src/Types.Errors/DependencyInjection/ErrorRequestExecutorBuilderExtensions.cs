using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides global configuration methods for error conventions to the <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class ErrorRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a type discoverer that can handle types implementing <see cref="IFieldResult"/>.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder AddFieldResultTypeDiscovery(
        this IRequestExecutorBuilder builder)
        => builder.AddTypeDiscoveryHandler(c => new FieldResultTypeDiscoveryHandler(c.TypeInspector));

    /// <summary>
    /// Defines the common interface that all errors implement.
    /// To specify the interface you can either provide a interface runtime type or a HotChocolate
    /// interface schema type.
    ///
    /// This has to be used together with <see cref="ErrorAttribute"/>  or
    /// <see cref="ErrorObjectFieldDescriptorExtensions.Error"/>
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <typeparam name="T">
    /// The type that is used as the common interface
    /// </typeparam>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder AddErrorInterfaceType<T>(
        this IRequestExecutorBuilder builder)
        => builder.ConfigureSchema(x => x.AddErrorInterfaceType<T>());

    /// <summary>
    /// Defines the common interface that all errors implement.
    /// To specify the interface you can either provide a interface runtime type or a HotChocolate
    /// interface schema type.
    ///
    /// This has to be used together with <see cref="ErrorAttribute"/>  or
    /// <see cref="ErrorObjectFieldDescriptorExtensions.Error"/>
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="type">
    /// The type that is used as the common interface
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder AddErrorInterfaceType(
        this IRequestExecutorBuilder builder,
        Type type) =>
        builder.ConfigureSchema(x => x.AddErrorInterfaceType(type));

    /// <summary>
    /// Adds a new error registrar for mutation errors.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <typeparam name="T">
    /// The mutation error registrar.
    /// </typeparam>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder AddMutationErrorConfiguration<T>(this IRequestExecutorBuilder builder)
        where T : MutationErrorConfiguration, new()
        => builder.TryAddTypeInterceptor(new MutationErrorTypeInterceptor<T>(new T()));

    /// <summary>
    /// Adds a new error registrar for mutation errors.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configuration">
    /// The mutation error configuration.
    /// </param>
    /// <typeparam name="T">
    /// The mutation error registrar.
    /// </typeparam>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder AddMutationErrorConfiguration<T>(
        this IRequestExecutorBuilder builder,
        T configuration)
        where T : MutationErrorConfiguration
        => builder.TryAddTypeInterceptor(new MutationErrorTypeInterceptor<T>(configuration));
}
