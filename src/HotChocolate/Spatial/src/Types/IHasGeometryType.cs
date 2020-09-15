namespace HotChocolate.Types.Spatial
{
    public interface IHasGeometryType
    {
        public GeoJsonGeometryType GeometryType { get; }
    }
}
