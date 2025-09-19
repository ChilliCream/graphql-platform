using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a type module to the schema.
    /// </summary>
    /// <typeparam name="T">
    /// The type module.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    public static IRequestExecutorBuilder AddTypeModule<T>(
        this IRequestExecutorBuilder builder)
        where T : ITypeModule
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Configure((sp, c) => c.TypeModules.Add(sp.GetRequiredService<T>()));
    }

    /// <summary>
    /// Adds a type module to the schema.
    /// </summary>
    /// <typeparam name="T">
    /// The type module.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="factory">
    /// The factory.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    public static IRequestExecutorBuilder AddTypeModule<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : ITypeModule
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.Configure((sp, c) => c.TypeModules.Add(factory(sp)));
    }
}
