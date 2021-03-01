using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Transformation
{
    /// <summary>
    /// Transforms a geometry into a another coordinates system
    /// </summary>
    public interface IGeometryTransformer
    {
        /// <summary>
        /// Transform the geometry into a coordinate system
        ///
        /// This method transforms the coordinate in place. The instance of the coordinate
        /// will stay the same.
        /// </summary>
        /// <param name="geometry">The geometry that should be transformed</param>
        /// <param name="targetSrid">The coordinate system it should be transformed to</param>
        void TransformInPlace(Geometry geometry, int targetSrid);
    }
}
