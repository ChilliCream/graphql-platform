using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Types;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class GeoJSONLineStringType : ObjectType<LineString>
    {
        protected override void Configure(IObjectTypeDescriptor<LineString> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor.Implements<GeoJSONInterface>();

            descriptor.Field("type").Resolver(GeoJSONGeometryType.LineString);
            descriptor.Field(x => x.Coordinates);
            descriptor.Field<Resolver>(x => x.GetBbox(default!));
        }

        internal class Resolver
        {
            public IReadOnlyCollection<double> GetBbox([Parent] LineString geometry)
            {
                var envelope = geometry.EnvelopeInternal;

                // TODO: support Z
                return new[] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY };
            }
        }
    }
}
