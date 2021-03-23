using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Spatial.Configuration;

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
        /// <param name="conventionFactory">
        /// Creates the convention for spatial types
        /// </param>
        /// <returns>
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder AddSpatialTypes(
            this IRequestExecutorBuilder builder,
            Func<SpatialConvention> conventionFactory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder
                .ConfigureSchema(x => x.AddSpatialTypes(conventionFactory));
        }

        /// <summary>
        /// Adds GeoJSON compliant spatial types.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="descriptor">
        /// Configure the spatial convention
        /// </param>
        /// <returns>
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder AddSpatialTypes(
            this IRequestExecutorBuilder builder,
            Action<ISpatialConventionDescriptor> descriptor)
        {
            return builder.AddSpatialTypes(() => new SpatialConvention(descriptor));
        }

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
        public static IRequestExecutorBuilder AddSpatialTypes(this IRequestExecutorBuilder builder)
        {
            return builder.AddSpatialTypes(() => new SpatialConvention());
        }
    }
}
