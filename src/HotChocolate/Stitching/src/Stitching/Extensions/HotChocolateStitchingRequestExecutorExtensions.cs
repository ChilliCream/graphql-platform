using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Stitching.Pipeline;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateStitchingRequestExecutorExtensions
    {
        /// <summary>
        /// This middleware delegates GraphQL requests to a different GraphQL server using
        /// GraphQL HTTP requests.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder UseHttpRequests(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseRequest<HttpRequestMiddleware>();
        }
    }
}
