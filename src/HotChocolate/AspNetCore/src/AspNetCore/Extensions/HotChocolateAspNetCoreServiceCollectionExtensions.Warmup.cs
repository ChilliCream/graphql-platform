using System;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HotChocolateAspNetCoreServiceCollectionExtensions
    {
        public static IRequestExecutorBuilder InitializeOnStartup(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddHostedService<ExecutorWarmupTask>();
            builder.Services.AddSingleton(new WarmupSchema(builder.Name));
            return builder;
        }
    }
}
