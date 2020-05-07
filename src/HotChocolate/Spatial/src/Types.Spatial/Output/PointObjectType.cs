using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using NetTopologySuite.Geometries;
using Types.Spatial.Common;

namespace Types.Spatial.Output
{
    public class PointObjectType : ObjectType<Point>
    {
        protected override void Configure(IObjectTypeDescriptor<Point> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Implements<GeoJSONInterface>();

            descriptor.Field("type")
                .Resolver(() => GeoJSONGeometryType.Point);

            descriptor.Field("bbox")
                .Resolver(BBoxResolver);

            descriptor.Field("coordinates")
                .Resolver((ctx) => ctx.Parent<Point>().Coordinates);

            descriptor.Field("crs")
                .Resolver(() => "urn:ogc:def:crs:OGC::CRS84");
        }

        private IReadOnlyCollection<double> BBoxResolver(IResolverContext context)
        {
            var envelope = context.Parent<Point>().EnvelopeInternal;

            return new [] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY };
        }
    }
}
