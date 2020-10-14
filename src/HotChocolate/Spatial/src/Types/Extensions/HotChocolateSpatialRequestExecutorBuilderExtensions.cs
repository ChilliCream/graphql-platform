using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extensions to the <see cref="IRequestExecutorBuilder"/>.
    /// </summary>
    public static class HotChocolateSpatialRequestExecutorBuilderExtensions
    {
        /// <summary>
        /// Adds GeoJSON compliant spatial types.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder AddSpatialTypes(
            this IRequestExecutorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(x => x.AddSpatialTypes());
        }
    }
}
