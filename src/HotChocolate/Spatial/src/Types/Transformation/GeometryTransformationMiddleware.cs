using System;
using System.Collections;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Transformation
{
    /// <summary>
    /// Transforms a Geometry to the SRID provided by the CRS argument of the field
    /// </summary>
    internal class GeometryTransformationMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly IGeometryTransformerFactory _factory;
        private readonly int _defaultSrid;

        public GeometryTransformationMiddleware(
            FieldDelegate next,
            IGeometryTransformerFactory factory,
            int defaultSrid)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _defaultSrid = defaultSrid;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            var targetCrs = context.ArgumentValue<int?>(WellKnownFields.CrsFieldName);
            if (targetCrs.HasValue)
            {
                TransformInPlace(context.Result, targetCrs.Value);
            }
        }

        private void TransformInPlace(
            object? runtimeValue,
            int toSrid)
        {
            if (runtimeValue is Geometry g)
            {
                if (g.SRID != toSrid)
                {
                    IGeometryTransformer transformer =
                        _factory.Create(g.SRID is -1 or 0 ? _defaultSrid : g.SRID, toSrid);
                    transformer.TransformInPlace(g, toSrid);
                }
            }
            else if (runtimeValue is IList list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    TransformInPlace(list[i], toSrid);
                }
            }
        }
    }
}
