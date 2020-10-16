using System;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
    {
        public static IRequestExecutorBuilder AddSocketSessionInterceptor<T>(
            this IRequestExecutorBuilder builder)
            where T : class, ISocketSessionInterceptor =>
            builder.ConfigureSchemaServices(s => s
                .RemoveAll<ISocketSessionInterceptor>()
                .AddSingleton<ISocketSessionInterceptor, T>());


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
