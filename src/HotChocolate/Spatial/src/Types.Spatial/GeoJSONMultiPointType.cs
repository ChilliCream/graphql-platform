using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONMultiPointType : ObjectType<MultiPoint>
    {
        protected override void Configure(IObjectTypeDescriptor<MultiPoint> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Implements<GeoJSONInterface>();

            descriptor.Field("type").Resolver(GeoJSONGeometryType.MultiPoint);
            descriptor.Field(x => x.Coordinates);
            descriptor.Field<Resolver>(x => x.GetBbox(default!));
        }

        internal class Resolver
        {
            public IReadOnlyCollection<double> GetBbox([Parent] MultiPoint geometry)
            {
                var envelope = geometry.EnvelopeInternal;

                // TODO: support Z
                return new[] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY };
            }
        }
    }
}
