using System;
using HotChocolate.Types.Spatial;
using HotChocolate.Types.Spatial.Configuration;
using HotChocolate.Types.Spatial.Transformation;
using NetTopologySuite.Geometries;

namespace HotChocolate
{
    /// <summary>
    /// Provides extensions to <see cref="ISchemaBuilder"/>.
    /// </summary>
    public static class SpatialSchemaBuilderExtensions
    {
        /// <summary>
        /// Adds GeoJSON compliant spatial types.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <param name="conventionFactory">
        /// Creates the convention for spatial types
        /// </param>
        /// <returns>
        /// The <see cref="ISchemaBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static ISchemaBuilder AddSpatialTypes(
            this ISchemaBuilder builder,
            Func<SpatialConvention> conventionFactory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder
                .TryAddConvention<ISpatialConvention>(conventionFactory())
                .TryAddTypeInterceptor<GeometryTransformerInterceptor>()
                .AddType<GeoJsonInterfaceType>()
                .AddType<GeoJsonGeometryType>()
                .AddType<GeoJsonPointInputType>()
                .AddType<GeoJsonMultiPointInputType>()
                .AddType<GeoJsonLineStringInputType>()
                .AddType<GeoJsonMultiLineStringInputType>()
                .AddType<GeoJsonPolygonInputType>()
                .AddType<GeoJsonMultiPolygonInputType>()
                .AddType<GeoJsonPointType>()
                .AddType<GeoJsonMultiPointType>()
                .AddType<GeoJsonLineStringType>()
                .AddType<GeoJsonMultiLineStringType>()
                .AddType<GeoJsonPolygonType>()
                .AddType<GeoJsonMultiPolygonType>()
                .AddType<GeoJsonGeometryEnumType>()
                .AddType<GeometryType>()
                .BindClrType<Coordinate, GeoJsonPositionType>();
        }

        /// <summary>
        /// Adds GeoJSON compliant spatial types.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <param name="descriptor">
        /// Configure the spatial convention
        /// </param>
        /// <returns>
        /// The <see cref="ISchemaBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static ISchemaBuilder AddSpatialTypes(
            this ISchemaBuilder builder,
            Action<ISpatialConventionDescriptor> descriptor)
        {
            return builder.AddSpatialTypes(() => new SpatialConvention(descriptor));
        }

        /// <summary>
        /// Adds GeoJSON compliant spatial types.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <returns>
        /// The <see cref="ISchemaBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static ISchemaBuilder AddSpatialTypes(this ISchemaBuilder builder)
        {
            return builder.AddSpatialTypes(() => new SpatialConvention());
        }
    }
}
