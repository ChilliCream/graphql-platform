using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
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

        if (!builder.Services.Any(t =>
            t.ServiceType == typeof(INodeIdValueSerializer)
            && t.ImplementationType == typeof(StringNodeIdValueSerializer)))
        {
            builder.Services.AddSingleton<INodeIdValueSerializer, StringNodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer, Int16NodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer, Int32NodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer, Int64NodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer, GuidNodeIdValueSerializer>();
        }

        builder.Services.TryAddSingleton<INodeIdSerializer>(sp =>
        {
            var allSerializers = sp.GetServices<INodeIdValueSerializer>().ToArray();
            return new DefaultNodeIdSerializer(allSerializers, maxIdLength);
        });

        builder.ConfigureSchemaServices(
            services =>
            {
                services.TryAddSingleton<INodeIdSerializer>(sp =>
                {
                    var schema = sp.GetRequiredService<ISchema>();
                    var boundSerializers = new List<BoundNodeIdValueSerializer>();
                    var allSerializers = sp.GetApplicationServices().GetServices<INodeIdValueSerializer>().ToArray();

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

                    return new OptimizedNodeIdSerializer(boundSerializers, allSerializers, maxIdLength);
                });
            });
        return builder;
    }

    /// <summary>
    /// Adds the legacy node id serializer to the schema.
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
    public static IRequestExecutorBuilder AddLegacyNodeIdSerializer(
        this IRequestExecutorBuilder builder,
        int maxIdLength = 1024)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.RemoveService<INodeIdSerializer>();
        builder.Services.TryAddSingleton<INodeIdSerializer, LegacyNodeIdSerializer>();

        builder.ConfigureSchemaServices(
            services =>
            {
                builder.Services.RemoveService<INodeIdSerializer>();
                services.TryAddSingleton<INodeIdSerializer, LegacyNodeIdSerializer>();
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

        builder.Services.AddSingleton<INodeIdValueSerializer, T>();
        return builder;
    }

    /// <summary>
    /// Adds a custom node id value serializer to the schema.
    /// A value serializer is used to format a runtime value into a node id.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="serializer">
    /// The node id value serializer instance.
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
        this IRequestExecutorBuilder builder,
        T serializer)
        where T : class, INodeIdValueSerializer
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<INodeIdValueSerializer>(serializer);
        return builder;
    }
}
