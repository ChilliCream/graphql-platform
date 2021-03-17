using System;
using HotChocolate.AspNetCore.Warmup;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the current GraphQL configuration to the warmup background service.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <see cref="IRequestExecutorBuilder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder InitializeOnStartup(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddHostedService<ExecutorWarmupService>();
            builder.Services.AddSingleton(new WarmupSchema(builder.Name));
            return builder;
        }
    }
}
