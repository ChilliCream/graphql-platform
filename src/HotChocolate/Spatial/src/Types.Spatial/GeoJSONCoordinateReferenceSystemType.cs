using System;
using HotChocolate.Types;

namespace Types.Spatial
{
    public class GeoJSONCoordinateReferenceSystemType
        : ObjectType<GeoJSONCoordinateReferenceSystem>
    {
        protected override void Configure(
            IObjectTypeDescriptor<GeoJSONCoordinateReferenceSystem> descriptor)
        {
            descriptor.BindFieldsImplicitly();
        }
    }
}
