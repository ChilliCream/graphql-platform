using System.Collections.Generic;
using GeoAPI.CoordinateSystems;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using NetTopologySuite.Geometries;
using Types.Spatial.Common;

namespace Types.Spatial.Output
{
    public class CRSObjectType : ObjectType<CRS>
    {
        protected override void Configure(IObjectTypeDescriptor<CRS> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Field("type")
                .Resolver(() => GeoJSONGeometryType.Point);

            descriptor.Field("bbox")
                .Resolver(BBoxResolver);

            descriptor.Field("coordinates")
                .Resolver((ctx) => ctx.Parent<Point>().Coordinates);

            descriptor.Field("crs")
                .Resolver(() => "urn:ogc:def:crs:OGC::CRS84");

    }
}
