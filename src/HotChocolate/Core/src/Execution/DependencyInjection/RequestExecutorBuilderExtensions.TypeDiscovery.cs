using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Registers a new type discovery handler with the type initialization.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// The factory to create the type discovery handler.
    /// </param>
    /// <typeparam name="T">
    /// The type discovery handler type.
    /// </typeparam>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="builder"/> is <c>null</c>
    /// - <paramref name="factory"/> is <c>null</c>
    /// </exception>
    public static IRequestExecutorBuilder AddTypeDiscoveryHandler<T>(
        this IRequestExecutorBuilder builder,
        Func<IDescriptorContext, T> factory)
        where T : TypeDiscoveryHandler
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        builder.ConfigureSchema(
            b => b.SetContextData(
                WellKnownContextData.TypeDiscoveryHandlers,
                value =>
                {
                    if (value is not List<Func<IDescriptorContext, TypeDiscoveryHandler>> list)
                    {
                        list = [];
                    }

                    list.Add(factory);
                    return list;
                }));

        return builder;
    }
}
