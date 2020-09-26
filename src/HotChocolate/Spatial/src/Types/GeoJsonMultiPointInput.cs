using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeoJsonMultiPointInput : GeoJsonInputObjectType<MultiPoint>
    {
        public GeoJsonMultiPointInput() : base(GeoJsonGeometryType.MultiPoint)
        {
        }

        protected override void Configure(IInputObjectTypeDescriptor<MultiPoint> descriptor)
        {
            descriptor.Name(MultiPointInputName);

            descriptor.BindFieldsExplicitly();

            descriptor
                .Field(TypeFieldName)
                .Type<GeoJsonGeometryEnumType>()
                .Description(GeoJson_Field_Type_Description);

            descriptor
                .Field(CoordinatesFieldName)
                .Type<ListType<GeoJsonPositionType>>()
                .Description(GeoJson_Field_Coordinates_Description_MultiPoint);

            descriptor
                .Field(CrsFieldName)
                .Type<IntType>()
                .Description(GeoJson_Field_Crs_Description);
        }
    }
}
