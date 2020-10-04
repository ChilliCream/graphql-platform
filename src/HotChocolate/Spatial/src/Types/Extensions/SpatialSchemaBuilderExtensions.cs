using System;
using HotChocolate.Types.Spatial;
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
        /// <returns>
        /// The <see cref="ISchemaBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static ISchemaBuilder AddSpatialTypes(this ISchemaBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder
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
    }
}
