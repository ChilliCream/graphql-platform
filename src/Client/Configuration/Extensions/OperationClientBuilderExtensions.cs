using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StrawberryShake.Configuration;

namespace StrawberryShake
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IOperationClientBuilder"/>
    /// </summary>
    public static class OperationClientBuilderExtensions
    {
        /// <summary>
        /// Configures the client options that will be used to create a operation client.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configure">
        /// A delegate that is used to configure the <see cref="ClientOptions"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IOperationClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IOperationClientBuilder ConfigureClient(
            this IOperationClientBuilder builder,
            Action<ClientOptionsModifiers> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure<ClientOptionsModifiers>(
                builder.Name,
                options => configure(options));

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="WebSocketClient"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="WebSocketClient"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
        /// </returns>
        /// <remarks>
        /// The <see cref="IServiceProvider"/> provided to <paramref name="configureClient"/>
        /// will be the application's root service provider instance.
        /// </remarks>
        public static IOperationClientBuilder ConfigureClient(
            this IOperationClientBuilder builder,
            Action<IServiceProvider, ClientOptionsModifiers> configureClient)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            builder.Services.AddTransient<IConfigureOptions<ClientOptionsModifiers>>(sp =>
                new ConfigureNamedOptions<ClientOptionsModifiers>(
                    builder.Name,
                    options => configureClient(sp, options)));

            return builder;
        }

        public static IOperationClientBuilder AddValueSerializer(
            this IOperationClientBuilder builder,
            Func<IServiceProvider, IValueSerializer> factory) =>
            builder.ConfigureClient((sp, o) =>
                o.ValueSerializers.Add(serializers =>
                {
                    IValueSerializer serializer = factory(sp);
                    serializers[serializer.Name] = serializer;
                }));

        public static IOperationClientBuilder AddValueSerializer(
            this IOperationClientBuilder builder,
            Func<IValueSerializer> factory) =>
            builder.ConfigureClient(o =>
                o.ValueSerializers.Add(serializers =>
                {
                    IValueSerializer serializer = factory();
                    serializers[serializer.Name] = serializer;
                }));

        public static IOperationClientBuilder AddResultParser(
            this IOperationClientBuilder builder,
            Func<IServiceProvider, IValueSerializerCollection, IResultParser> factory) =>
            builder.ConfigureClient((sp, o) =>
                o.ResultParsers.Add((serializers, parsers) =>
                {
                    IResultParser parser = factory(sp, serializers);
                    parsers[parser.ResultType] = parser;
                }));

        public static IOperationClientBuilder AddResultParser(
            this IOperationClientBuilder builder,
            Func<IValueSerializerCollection, IResultParser> factory) =>
            builder.ConfigureClient(o =>
                o.ResultParsers.Add((serializers, parsers) =>
                {
                    IResultParser parser = factory(serializers);
                    parsers[parser.ResultType] = parser;
                }));

        public static IOperationClientBuilder AddOperationFormatter(
            this IOperationClientBuilder builder,
            Func<IServiceProvider, IValueSerializerCollection, IOperationFormatter> factory) =>
            builder.ConfigureClient((sp, o) =>
                o.OperationFormatter = serializer =>
                    factory(sp, serializer));

        public static IOperationClientBuilder AddOperationFormatter(
            this IOperationClientBuilder builder,
            Func<IValueSerializerCollection, IOperationFormatter> factory) =>
            builder.ConfigureClient(o =>
                o.OperationFormatter = serializer =>
                    factory(serializer));

        public static IOperationClientBuilder AddOperationPipeline<T>(
            this IOperationClientBuilder builder,
            Func<IServiceProvider, OperationDelegate<T>> factory)
            where T : IOperationContext =>
            builder.ConfigureClient((sp, o) =>
                o.OperationPipelines.Add(pipelines =>
                    pipelines.Add(factory(sp))));

        public static IOperationClientBuilder AddOperationPipeline<T>(
            this IOperationClientBuilder builder,
            Func<OperationDelegate<T>> factory)
            where T : IOperationContext =>
            builder.ConfigureClient(o =>
                o.OperationPipelines.Add(pipelines =>
                    pipelines.Add(factory())));
    }
}
