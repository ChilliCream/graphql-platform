using System;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Internal;
using Grpc.AspNetCore.Server.Model;
using Grpc.AspNetCore.Server.Model.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the gRPC services.
    /// </summary>
    public static class GrpcServicesExtensions
    {
        /// <summary>
        /// Adds gRPC services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <param name="configureOptions">An <see cref="Action{GrpcServiceOptions}"/> to configure the provided <see cref="GrpcServiceOptions"/>.</param>
        /// <returns>An <see cref="IGrpcServerBuilder"/> that can be used to further configure the gRPC services.</returns>
        public static IGrpcServerBuilder AddGraphQLOverGrpc(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddGrpc();
        }

        /// <summary>
        /// Adds gRPC services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <param name="configureOptions">An <see cref="Action{GrpcServiceOptions}"/> to configure the provided <see cref="GrpcServiceOptions"/>.</param>
        /// <returns>An <see cref="IGrpcServerBuilder"/> that can be used to further configure the gRPC services.</returns>
        public static IGrpcServerBuilder AddGrpc0(this IServiceCollection services, Action<GrpcServiceOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.Configure(configureOptions).AddGrpc();
        }
    }
}
