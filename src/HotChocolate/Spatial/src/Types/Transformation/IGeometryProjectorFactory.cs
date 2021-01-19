using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Transformation
{
    public interface IGeometryProjectorFactory
    {
        IGeometryProjector Create(Geometry geometry, int targetSrid);

        bool HasCoordinateSystems();

        bool ContainsCoordinateSystem(int srid);
    }
}
