using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems.Transformations;

namespace HotChocolate.Types.Spatial.Transformation
{
    /// <inheritdoc />
    internal class GeometryTransformer
        : IGeometryTransformer
    {
        private readonly ICoordinateTransformation _transformation;

        public GeometryTransformer(ICoordinateTransformation transformation)
        {
            _transformation = transformation;
        }

        /// <inheritdoc />
        public void TransformInPlace(Geometry geometry, int targetSrid)
        {
            bool hasErrors = false;

            // This function transforms the coordinate in place. The instance of the coordinate
            // will stay the same.
            void TransformCoordinateInPlace(Coordinate coordinate)
            {
                double x;
                double y;
                double z;

                switch (coordinate)
                {
                    case CoordinateZM:
                    case CoordinateM:
                        hasErrors = true;
                        break;
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

            Parallel.ForEach(geometry.Coordinates, TransformCoordinateInPlace);

            if (hasErrors)
            {
                throw ThrowHelper.Transformation_CoordinateMNotSupported();
            }

            geometry.SRID = targetSrid;
        }
    }
}
