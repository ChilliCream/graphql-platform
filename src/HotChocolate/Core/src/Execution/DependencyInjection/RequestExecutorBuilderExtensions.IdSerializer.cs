using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a default node id serializer to the schema.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="maxIdLength">
    /// The maximum allowed length of a node id.
    /// </param>
    /// <returns>
    /// Returns the request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static IRequestExecutorBuilder AddDefaultNodeIdSerializer(
        this IRequestExecutorBuilder builder,
        int maxIdLength = 1024)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.ConfigureSchemaServices(
            services =>
            {
                services.TryAddSingleton<INodeIdSerializer>(sp =>
                {
                    var schema = sp.GetRequiredService<ISchema>();
                    var boundSerializers = new List<BoundNodeIdValueSerializer>();
                    var allSerializers = sp.GetServices<INodeIdValueSerializer>().ToArray();

                    if (schema.ContextData.TryGetValue(WellKnownContextData.SerializerTypes, out var value))
                    {
                        var serializerTypes = (Dictionary<string, Type>)value!;

                        foreach (var item in serializerTypes)
                        {
                            foreach (var serializer in allSerializers)
                            {
                                if (serializer.IsSupported(item.Value))
                                {
                                    boundSerializers.Add(new BoundNodeIdValueSerializer(item.Key, serializer));
                                    break;
                                }
                            }
                        }
                    }

                    return new DefaultNodeIdSerializer(boundSerializers, allSerializers, maxIdLength);
                });
            });
        return builder;
    }

    /// <summary>
    /// Adds a custom node id value serializer to the schema.
    /// A value serializer is used to format a runtime value into a node id.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <typeparam name="T">
    /// The type of the node id value serializer.
    /// </typeparam>
    /// <returns>
    /// Returns the request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static IRequestExecutorBuilder AddNodeIdValueSerializer<T>(
        this IRequestExecutorBuilder builder)
        where T : class, INodeIdValueSerializer
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.ConfigureSchemaServices(
            services => services.AddSingleton<INodeIdValueSerializer, T>());
        return builder;
    }
}
