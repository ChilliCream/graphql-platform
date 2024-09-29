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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(typeInterceptor);

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeInterceptor));
    }

    public static IRequestExecutorBuilder TryAddTypeInterceptor(
        this IRequestExecutorBuilder builder,
        Type typeInterceptor)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(typeInterceptor);

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeInterceptor));
    }

    public static IRequestExecutorBuilder TryAddTypeInterceptor<T>(
        this IRequestExecutorBuilder builder)
        where T : TypeInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeof(T)));
    }
}
