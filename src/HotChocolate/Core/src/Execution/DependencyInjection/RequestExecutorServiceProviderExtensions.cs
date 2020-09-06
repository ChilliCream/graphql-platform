using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extensions for <see cref="IServiceProvider" /> to interact with the 
    /// <see cref="IRequestExecutor" />.
    /// </summary>
    public static class RequestExecutorServiceProviderExtensions
    {
        /// <summary>
        /// Gets the <see cref="IRequestExecutor" /> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceProvider"/>.
        /// </param>
        /// <param name="schemaName">
        /// The schema name.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutor" />.
        /// </returns>
        public static ValueTask<IRequestExecutor> GetRequestExecutorAsync(
            this IServiceProvider services,
            NameString schemaName = default, 
            CancellationToken cancellationToken = default) =>
            services
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync(schemaName, cancellationToken);
    }
}