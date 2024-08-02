using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddTypeModule<T>(
        this IRequestExecutorBuilder builder)
        where T : ITypeModule
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.Configure((sp, c) => c.TypeModules.Add(sp.GetRequiredService<T>()));
    }

    public static IRequestExecutorBuilder AddTypeModule<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : ITypeModule
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return builder.Configure((sp, c) => c.TypeModules.Add(factory(sp)));
    }
}
