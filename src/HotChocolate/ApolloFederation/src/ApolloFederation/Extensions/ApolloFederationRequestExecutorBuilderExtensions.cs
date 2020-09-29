using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApolloFederationRequestExecutorBuilderExtensions
    {
        /// <summary>
        /// Adds support for Apollo Federation to the schema.
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
        public static IRequestExecutorBuilder AddApolloFederation(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(s => s.AddApolloFederation());
        }
    }
}
