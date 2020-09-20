using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Utilities;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddSelectionSetOptimizer<T>(
            this IRequestExecutorBuilder builder)
            where T : class, ISelectionSetOptimizer
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ConfigureSchemaServices(s => s.AddSingleton<ISelectionSetOptimizer, T>());
            return builder;
        }

        public static IRequestExecutorBuilder AddSelectionSetOptimizer<T>(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, T> factory)
            where T : class, ISelectionSetOptimizer
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ConfigureSchemaServices(
                sc => sc.AddSingleton<ISelectionSetOptimizer>(factory));
            return builder;
        }
    }
}
