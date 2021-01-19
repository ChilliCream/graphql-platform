using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Transformation
{
    public interface IGeometryProjector
    {
        void Reproject(Geometry geometry, int targetSrid);
    }
}
