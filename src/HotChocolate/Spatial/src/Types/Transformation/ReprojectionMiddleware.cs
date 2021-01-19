using System;
using System.Collections;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Transformation
{
    internal class ReprojectionMiddleware
    {
        private readonly FieldDelegate _next;
        private IGeometryProjectorFactory _factory;

        public ReprojectionMiddleware(
            FieldDelegate next,
            IGeometryProjectorFactory factory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            var targetCrs = context.ArgumentValue<int?>(WellKnownFields.CrsFieldName);
            if (targetCrs.HasValue)
            {
                ProjectGeometry(context.Result, targetCrs.Value);
            }
        }

        public void ProjectGeometry(object? runtimeValue, int targetCrs)
        {
            if (runtimeValue is Geometry g)
            {
                if (g.SRID != targetCrs)
                {
                    IGeometryProjector projector = _factory.Create(g, targetCrs);
                    projector.Reproject(g, targetCrs);
                }
            }
            else if (runtimeValue is IList list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    ProjectGeometry(list[i], targetCrs);
                }
            }
        }
    }
}
