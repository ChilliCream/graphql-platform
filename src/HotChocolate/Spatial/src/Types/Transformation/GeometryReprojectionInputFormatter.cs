using System.Collections;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Transformation
{
    internal class GeometryReprojectionInputFormatter : IInputValueFormatter
    {
        private IGeometryProjectorFactory _factory;
        private int _targetCrs;

        public GeometryReprojectionInputFormatter(
            IGeometryProjectorFactory factory,
            int targetCrs)
        {
            _factory = factory;
            _targetCrs = targetCrs;
        }

        public object? OnAfterDeserialize(object? runtimeValue)
        {
            if (runtimeValue is Geometry g)
            {
                if (g.SRID != -1)
                {
                    IGeometryProjector projector = _factory.Create(g, _targetCrs);
                    projector.Reproject(g, _targetCrs);
                }
            }
            else if (runtimeValue is IList list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    OnAfterDeserialize(list[i]);
                }
            }

            return runtimeValue;
        }
    }
}
