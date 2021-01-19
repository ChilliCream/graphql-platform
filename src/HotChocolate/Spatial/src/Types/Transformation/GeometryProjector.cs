using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems.Transformations;

namespace HotChocolate.Types.Spatial.Transformation
{
    internal class GeometryProjector
        : IGeometryProjector
    {
        private ICoordinateTransformation _transformation;

        public GeometryProjector(ICoordinateTransformation transformation)
        {
            _transformation = transformation;
        }

        public void Reproject(Geometry geometry, int targetSrid)
        {
            Parallel.ForEach(geometry.Coordinates, AdjustCoordinate);
            geometry.SRID = targetSrid;
        }

        private void AdjustCoordinate(Coordinate coordinate)
        {
            double x;
            double y;
            double z;

            switch (coordinate)
            {
                case CoordinateZM:
                    throw ThrowHelper.Transformation_Projection_CoodinateMNotSupported();
                case CoordinateM:
                    throw ThrowHelper.Transformation_Projection_CoodinateMNotSupported();
                case CoordinateZ:
                    x = coordinate.X;
                    y = coordinate.Y;
                    z = coordinate.Z;
                    _transformation.MathTransform.Transform(ref x, ref y, ref z);
                    coordinate.X = x;
                    coordinate.Y = y;
                    coordinate.Z = z;
                    break;
                default:
                    x = coordinate.X;
                    y = coordinate.Y;
                    _transformation.MathTransform.Transform(ref x, ref y);
                    coordinate.X = x;
                    coordinate.Y = y;
                    break;
            }
        }
    }
}
