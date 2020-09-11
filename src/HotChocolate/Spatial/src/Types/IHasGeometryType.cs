namespace HotChocolate.Types.Spatial
{
    public interface IHasGeometryType
    {
        public GeoJSONGeometryType GeometryType { get; }
    }
}
