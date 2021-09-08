using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Stitching.Requests;
using HotChocolate.Stitching.DAPR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateStitchingDAPRRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddRemoteSchemasFromDAPR(
            this IRequestExecutorBuilder builder,
            NameString configurationName,
            string daprComponent)
        {
            configurationName.EnsureNotEmpty(nameof(configurationName));

            builder.Services.AddSingleton<IRequestExecutorOptionsProvider>(sp =>
            {
                return new DAPRExecutorOptionsProvider(builder.Name, configurationName);
            });

            // Last but not least, we will setup the stitching context which will
            // provide access to the remote executors which in turn use the just configured
            // request executor proxies to send requests to the downstream services.
            builder.Services.TryAddScoped<IStitchingContext, StitchingContext>();

            return builder;
        }
    }
}
