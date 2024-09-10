using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder TryAddTypeInterceptor(
        this IRequestExecutorBuilder builder,
        TypeInterceptor typeInterceptor)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeInterceptor is null)
        {
            throw new ArgumentNullException(nameof(typeInterceptor));
        }

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeInterceptor));
    }

    public static IRequestExecutorBuilder TryAddTypeInterceptor(
        this IRequestExecutorBuilder builder,
        Type typeInterceptor)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeInterceptor is null)
        {
            throw new ArgumentNullException(nameof(typeInterceptor));
        }

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeInterceptor));
    }

    public static IRequestExecutorBuilder TryAddTypeInterceptor<T>(
        this IRequestExecutorBuilder builder)
        where T : TypeInterceptor
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeof(T)));
    }
}
