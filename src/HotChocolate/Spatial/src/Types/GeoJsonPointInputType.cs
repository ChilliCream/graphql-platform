using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownTypeNames;

namespace HotChocolate.Types.Spatial
{
    public sealed class GeoJsonPointInputType : GeoJsonInputType<Point>
    {
        public GeoJsonPointInputType() : base(GeoJsonGeometryType.Point)
        {
        }

        protected override void Configure(IInputObjectTypeDescriptor<Point> descriptor)
        {
            descriptor
                .Name(PointInputName)
                .BindFieldsExplicitly();

            descriptor.Field(TypeFieldName)
                .Type<GeoJsonGeometryEnumType>()
                .Description(GeoJson_Field_Type_Description);

            descriptor.Field(CoordinatesFieldName)
                .Type<GeoJsonPositionType>()
                .Description(GeoJson_Field_Coordinates_Description_Point);

            descriptor.Field(CrsFieldName)
                .Type<IntType>()
                .Description(GeoJson_Field_Crs_Description);
        }
    }
}
