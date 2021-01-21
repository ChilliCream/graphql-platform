using System.Collections.Concurrent;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace HotChocolate.Types.Spatial.Transformation
{
    /// <inheritdoc />
    internal class GeometryTransformerFactory
        : IGeometryTransformerFactory
    {
        private readonly IReadOnlyDictionary<int, CoordinateSystem> _coordinateSystems;
        private readonly CoordinateTransformationFactory _factory = new();
        private readonly ConcurrentDictionary<(int From, int To), IGeometryTransformer>
            _transformations = new();

        public GeometryTransformerFactory(
            IReadOnlyDictionary<int, CoordinateSystem> coordinateSystems)
        {
            _coordinateSystems = coordinateSystems;
        }

        /// <inheritdoc />
        public bool HasCoordinateSystems()
        {
            return _coordinateSystems.Count > 0;
        }

        /// <inheritdoc />
        public IGeometryTransformer Create(int fromSrid, int toSrid)
        {
            if (fromSrid == -1)
            {
                return DefaultProjector.Default;
            }

            if (_transformations.TryGetValue(
                (fromSrid, toSrid),
                out IGeometryTransformer? transformation))
            {
                return transformation;
            }

            if (!_coordinateSystems.TryGetValue(fromSrid, out CoordinateSystem? fromCs))
            {
                throw ThrowHelper.Transformation_UnknownCRS(fromSrid);
            }

            if (!_coordinateSystems.TryGetValue(toSrid, out CoordinateSystem? toCs))
            {
                throw ThrowHelper.Transformation_UnknownCRS(fromSrid);
            }

            return _transformations.GetOrAdd(
                (fromSrid, toSrid),
                _ => new GeometryTransformer(_factory.CreateFromCoordinateSystems(fromCs, toCs)));
        }

        /// <inheritdoc />
        public bool ContainsCoordinateSystem(int srid)
        {
            return _coordinateSystems.ContainsKey(srid);
        }

        /// <summary>
        /// Default projector. Does not modify the geometry
        /// </summary>
        private class DefaultProjector : IGeometryTransformer
        {
            private DefaultProjector()
            {
            }

            /// <inheritdoc />
            public void TransformInPlace(Geometry geometry, int targetSrid)
            {
                // empty on purpose
            }

            /// <summary>
            /// The instance of the default projector
            /// </summary>
            public static readonly DefaultProjector Default = new();
        }
    }
}
