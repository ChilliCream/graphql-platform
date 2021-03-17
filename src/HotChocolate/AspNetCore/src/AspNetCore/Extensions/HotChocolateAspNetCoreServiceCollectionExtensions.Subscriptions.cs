using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an interceptor for GraphQL socket sessions to the GraphQL configuration.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <typeparam name="T">
        /// The <see cref="ISocketSessionInterceptor"/> implementation.
        /// </typeparam>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
        /// </returns>
        public static IRequestExecutorBuilder AddSocketSessionInterceptor<T>(
            this IRequestExecutorBuilder builder)
            where T : class, ISocketSessionInterceptor =>
            builder.ConfigureSchemaServices(s => s
                .RemoveAll<ISocketSessionInterceptor>()
                .AddSingleton<ISocketSessionInterceptor, T>());

        /// <summary>
        /// Adds an interceptor for GraphQL socket sessions to the GraphQL configuration.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="factory">
        /// A factory that creates the interceptor instance.
        /// </param>
        /// <typeparam name="T">
        /// The <see cref="ISocketSessionInterceptor"/> implementation.
        /// </typeparam>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
        /// </returns>
        public static IRequestExecutorBuilder AddSocketSessionInterceptor<T>(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, T> factory)
            where T : class, ISocketSessionInterceptor=>
            builder.ConfigureSchemaServices(s => s
                .RemoveAll<ISocketSessionInterceptor>()
                .AddSingleton<ISocketSessionInterceptor, T>(factory));

        private static IRequestExecutorBuilder AddSubscriptionServices(
            this IRequestExecutorBuilder builder)
        {
            return builder.ConfigureSchemaServices(s =>
            {
                s.TryAddSingleton<IMessagePipeline, DefaultMessagePipeline>();
                s.TryAddSingleton<ISocketSessionInterceptor, DefaultSocketSessionInterceptor>();

                s.AddSingleton<IMessageHandler, DataStartMessageHandler>();
                s.AddSingleton<IMessageHandler, DataStopMessageHandler>();
                s.AddSingleton<IMessageHandler, InitializeConnectionMessageHandler>();
                s.AddSingleton<IMessageHandler, TerminateConnectionMessageHandler>();
            });
        }
    }
}
