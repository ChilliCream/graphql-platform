#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Features;

namespace HotChocolate;

public static partial class SchemaBuilderExtensions
{
    /// <summary>
    /// Tries to add a type interceptor with the schema builder.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the type interceptor.
    /// </typeparam>
    /// <param name="builder">
    /// The schema builder.
    /// </param>
    /// <param name="factory">
    /// An optional factory function that creates the type interceptor.
    /// </param>
    /// <returns>The schema builder.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static ISchemaBuilder TryAddTypeInterceptor<T>(
        this ISchemaBuilder builder,
        Func<IServiceProvider, T>? factory = null)
        where T : TypeInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (factory is null)
        {
            builder.Features.GetOrSet<TypeInterceptorCollection>().TryAdd(typeof(T));
        }
        else
        {
            builder.Features.GetOrSet<TypeInterceptorCollection>().TryAdd(factory);
        }

        return builder.TryAddTypeInterceptor(typeof(T));
    }

    /// <summary>
    /// Tries to add a type interceptor with the schema builder.
    /// </summary>
    /// <param name="builder">
    /// The schema builder.
    /// </param>
    /// <param name="interceptorType">
    /// The type of the type interceptor.
    /// </param>
    /// <returns>The schema builder.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="interceptorType"/> is not a valid type interceptor.
    /// </exception>
    public static ISchemaBuilder TryAddTypeInterceptor(
        this ISchemaBuilder builder,
        Type interceptorType)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(interceptorType);

        if (!typeof(TypeInterceptor).IsAssignableFrom(interceptorType))
        {
            throw new ArgumentException(
                Properties.TypeResources.SchemaBuilder_Interceptor_NotSupported,
                nameof(interceptorType));
        }

        builder.Features.GetOrSet<TypeInterceptorCollection>().TryAdd(interceptorType);

        return builder;
    }

    /// <summary>
    /// Tries to add a type interceptor with the schema builder.
    /// </summary>
    /// <param name="builder">
    /// The schema builder.
    /// </param>
    /// <param name="interceptor">
    /// The type interceptor.
    /// </param>
    /// <param name="uniqueByType">
    /// If <c>true</c>, the type interceptor will be added only if it is not already registered.
    /// </param>
    /// <returns>The schema builder.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="interceptor"/> is <c>null</c>.
    /// </exception>
    public static ISchemaBuilder TryAddTypeInterceptor(
        this ISchemaBuilder builder,
        TypeInterceptor interceptor,
        bool uniqueByType = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(interceptor);

        builder.Features.GetOrSet<TypeInterceptorCollection>().TryAdd(interceptor, uniqueByType);

        return builder;
    }
}
