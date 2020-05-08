using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Types;
using NetTopologySuite.Geometries;
using Types.Spatial.Common;

namespace Types.Spatial.Output
{
    public class PointObjectExtensionType : ObjectTypeExtension<Point>
    {
        protected override void Configure(IObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field<CrsResolvers>(x => x.GetCrs(default!));
        }
    }

    public class CrsResolvers
    {
        public string GetCrs([Parent] Geometry geometry)
            => "urn:ogc:def:crs:OGC::CRS84";
    }

    public class PointObjectType : ObjectType<Point>
    {
        protected override void Configure(IObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Implements<GeoJSONInterface>();

            descriptor.Field("type").Resolver(GeoJSONGeometryType.Point);
            descriptor.Field(x => x.Coordinates);
            descriptor.Field<Resolver>(x => x.GetBbox(default!));
        }

        internal class Resolver
        {
            public IReadOnlyCollection<double> GetBbox([Parent] Point point)
            {
                var envelope = point.EnvelopeInternal;

                return new[] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY };
            }
        }
    }
}
