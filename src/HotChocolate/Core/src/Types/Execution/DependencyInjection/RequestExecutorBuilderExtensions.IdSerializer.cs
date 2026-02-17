using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Relay;
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
    /// <param name="outputNewIdFormat">
    /// Defines whether the new ID format shall be used when serializing IDs.
    /// </param>
    /// <param name="useUrlSafeBase64">
    /// Defines whether the new ID format shall use URL safe base64 encoding.
    /// </param>
    /// <returns>
    /// Returns the request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static IRequestExecutorBuilder AddDefaultNodeIdSerializer(
        this IRequestExecutorBuilder builder,
        int maxIdLength = 1024,
        bool outputNewIdFormat = true,
        bool useUrlSafeBase64 = false)
        => AddDefaultNodeIdSerializer(
            builder,
            new NodeIdSerializerOptions
            {
                MaxIdLength = maxIdLength,
                OutputNewIdFormat = outputNewIdFormat,
                Format = useUrlSafeBase64
                    ? NodeIdSerializerFormat.UrlSafeBase64
                    : NodeIdSerializerFormat.Base64
            });

    /// <summary>
    /// Adds a default node id serializer to the schema.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="options">
    /// The serializer options.
    /// </param>
    /// <returns>
    /// Returns the request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static IRequestExecutorBuilder AddDefaultNodeIdSerializer(
        this IRequestExecutorBuilder builder,
        NodeIdSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var outputNewIdFormat = options.OutputNewIdFormat;

        if (!builder.Services.IsImplementationTypeRegistered<StringNodeIdValueSerializer>())
        {
            builder.Services.AddSingleton<INodeIdValueSerializer, StringNodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer, Int16NodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer, Int32NodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer, Int64NodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer, DecimalNodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer, SingleNodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer, DoubleNodeIdValueSerializer>();
            builder.Services.AddSingleton<INodeIdValueSerializer>(new GuidNodeIdValueSerializer(outputNewIdFormat));
        }
        else
        {
            // if serializers are already registered we need to replace the
            // default guid serializer with the new one in order to make sure
            // that we have the right settings configured for it.
            var serviceRegistration = builder.Services.FirstOrDefault(
                t => t.ServiceType == typeof(INodeIdValueSerializer)
                    && t.ImplementationType == typeof(GuidNodeIdValueSerializer));
            if (serviceRegistration is not null)
            {
                builder.Services.Remove(serviceRegistration);
                builder.Services.AddSingleton<INodeIdValueSerializer>(new GuidNodeIdValueSerializer(outputNewIdFormat));
            }
        }

        builder.Services.RemoveService<INodeIdSerializer>();
        builder.Services.TryAddSingleton<INodeIdSerializer>(sp =>
        {
            var allSerializers = sp.GetServices<INodeIdValueSerializer>().ToArray();
            return new DefaultNodeIdSerializer(
                allSerializers,
                options.MaxIdLength,
                outputNewIdFormat,
                options.Format,
                options.MaxCachedTypeNames);
        });

        builder.ConfigureSchemaServices(
            services =>
            {
                services.RemoveService<INodeIdSerializer>();
                services.TryAddSingleton<INodeIdSerializer>(sp =>
                {
                    var schema = sp.GetRequiredService<Schema>();
                    var boundSerializers = new List<BoundNodeIdValueSerializer>();
                    var allSerializers = sp.GetRootServiceProvider().GetServices<INodeIdValueSerializer>().ToArray();
                    var feature = schema.Features.Get<NodeSchemaFeature>();

                    if (feature is not null)
                    {
                        var lookup = new Dictionary<Type, INodeIdValueSerializer>();
                        foreach (var (entityType, idType) in feature.NodeIdTypes)
                        {
                            if (lookup.TryGetValue(idType, out var serializer))
                            {
                                boundSerializers.Add(
                                    new BoundNodeIdValueSerializer(
                                        entityType,
                                        serializer));
                                continue;
                            }

                            foreach (var possibleSerializer in allSerializers)
                            {
                                if (possibleSerializer.IsSupported(idType))
                                {
                                    lookup[idType] = possibleSerializer;
                                    boundSerializers.Add(
                                        new BoundNodeIdValueSerializer(
                                            entityType,
                                            possibleSerializer));
                                    break;
                                }
                            }
                        }
                    }

                    return new OptimizedNodeIdSerializer(
                        boundSerializers,
                        allSerializers,
                        options.MaxIdLength,
                        outputNewIdFormat,
                        options.Format);
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
    /// <returns>
    /// Returns the request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static IRequestExecutorBuilder AddLegacyNodeIdSerializer(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.RemoveService<INodeIdSerializer>();
        builder.Services.TryAddSingleton<INodeIdSerializer, LegacyNodeIdSerializer>();

        builder.ConfigureSchemaServices(
            services =>
            {
                services.RemoveService<INodeIdSerializer>();
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
        ArgumentNullException.ThrowIfNull(builder);

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
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<INodeIdValueSerializer>(serializer);
        return builder;
    }
}
