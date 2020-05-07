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

            // TODO: implement coordinates resolver with coordinates Scalar
        }

        private List<float> BBoxResolver(IResolverContext context)
        {
            var point = context.Parent<Point>();

            // TODO: add logic
            return new List<float>();
        }
    }
}
