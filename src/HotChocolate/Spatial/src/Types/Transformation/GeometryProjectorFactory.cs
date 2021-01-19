using System.Collections.Concurrent;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace HotChocolate.Types.Spatial.Transformation
{
    internal class GeometryProjectorFactory
        : IGeometryProjectorFactory
    {
        private readonly IReadOnlyDictionary<int, CoordinateSystem> _coordinateSystems;
        private readonly CoordinateTransformationFactory _factory = new();
        private readonly ConcurrentDictionary<(int From, int To), IGeometryProjector>
            _transformations = new();

        public GeometryProjectorFactory(
            IReadOnlyDictionary<int, CoordinateSystem> coordinateSystems)
        {
            _coordinateSystems = coordinateSystems;
        }

        public bool HasCoordinateSystems()
        {
            return _coordinateSystems.Count > 0;
        }

        public IGeometryProjector Create(Geometry geometry, int targetSrid)
        {
            if (geometry.SRID == -1)
            {
                return DefaultProjector.Default;
            }

            if (_transformations.TryGetValue(
                (geometry.SRID, targetSrid),
                out IGeometryProjector? transformation))
            {
                return transformation;
            }

            if (!_coordinateSystems.TryGetValue(geometry.SRID, out CoordinateSystem? fromCs))
            {
                throw ThrowHelper.Transformation_UnknownCRS(geometry.SRID);
            }

            if (!_coordinateSystems.TryGetValue(targetSrid, out CoordinateSystem? toCs))
            {
                throw ThrowHelper.Transformation_UnknownCRS(geometry.SRID);
            }

            return _transformations.GetOrAdd(
                (geometry.SRID, targetSrid),
                x => new GeometryProjector(_factory.CreateFromCoordinateSystems(fromCs, toCs)));
        }

        public bool ContainsCoordinateSystem(int srid)
        {
            return _coordinateSystems.ContainsKey(srid);
        }

        private class DefaultProjector : IGeometryProjector
        {
            public void Reproject(Geometry geometry, int targetSrid)
            {
            }

            public static DefaultProjector Default = new();
        }
    }
}
