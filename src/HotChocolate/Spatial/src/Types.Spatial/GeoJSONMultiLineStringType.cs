using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONMultiLineStringType : ObjectType<MultiLineString>
    {
        protected override void Configure(IObjectTypeDescriptor<MultiLineString> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Implements<GeoJSONInterface>();

            descriptor.Field(x => x.Coordinates);
            descriptor.Field<GeoJSONResolvers>(x => x.GetType(default!));
            descriptor.Field<Resolver>(x => x.GetBbox(default!));
        }

        internal class Resolver
        {
            public IReadOnlyCollection<double> GetBbox([Parent] MultiLineString geometry)
            {
                var envelope = geometry.EnvelopeInternal;

                // TODO: support Z
                return new[] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY };
            }
        }
    }
}
