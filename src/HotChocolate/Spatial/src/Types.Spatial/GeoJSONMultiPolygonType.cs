using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONMultiPolygonType : ObjectType<MultiPolygon>
    {
        protected override void Configure(IObjectTypeDescriptor<MultiPolygon> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Implements<GeoJSONInterface>();

            descriptor.Field("type").Resolver(GeoJSONGeometryType.MultiPolygon);
            descriptor.Field(x => x.Coordinates);
            descriptor.Field<Resolver>(x => x.GetBbox(default!));
        }

        internal class Resolver
        {
            public IReadOnlyCollection<double> GetBbox([Parent] MultiPolygon geometry)
            {
                var envelope = geometry.EnvelopeInternal;

                // TODO: support Z
                return new[] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY };
            }
        }
    }
}
