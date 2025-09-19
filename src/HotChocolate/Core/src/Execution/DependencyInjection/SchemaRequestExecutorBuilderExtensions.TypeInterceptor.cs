using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a type interceptor to the schema.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="typeInterceptor">
    /// The type interceptor.
    /// </param>
    /// <param name="uniqueByType">
    /// If set to <c>true</c> the type interceptor will only be added once per type.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    public static IRequestExecutorBuilder TryAddTypeInterceptor(
        this IRequestExecutorBuilder builder,
        TypeInterceptor typeInterceptor,
        bool uniqueByType = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(typeInterceptor);

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeInterceptor, uniqueByType));
    }

    /// <summary>
    /// Adds a type interceptor to the schema.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="typeInterceptor">
    /// The type interceptor.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    public static IRequestExecutorBuilder TryAddTypeInterceptor(
        this IRequestExecutorBuilder builder,
        Type typeInterceptor)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(typeInterceptor);

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeInterceptor));
    }

    /// <summary>
    /// Adds a type interceptor to the schema.
    /// </summary>
    /// <typeparam name="T">
    /// The type interceptor.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="factory">
    /// An optional factory function to create the type interceptor.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    public static IRequestExecutorBuilder TryAddTypeInterceptor<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T>? factory = null)
        where T : TypeInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(factory));
    }
}
