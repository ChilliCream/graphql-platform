using System.Collections;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Transformation;

/// <summary>
/// Transforms a Geometry to the default SRID provided by the convention
/// </summary>
internal class GeometryTransformerInputFormatter : IInputValueFormatter
{
    private readonly IGeometryTransformerFactory _factory;
    private readonly int _targetCrs;

    public GeometryTransformerInputFormatter(
        IGeometryTransformerFactory factory,
        int targetCrs)
    {
        _factory = factory;
        _targetCrs = targetCrs;
    }

    /// <inheritdoc />
    public object? Format(object? originalValue)
    {
        TransformInPlace(originalValue, _targetCrs);
        return originalValue;
    }

    private void TransformInPlace(
        object? runtimeValue,
        int toSrid)
    {
        if (runtimeValue is Geometry g)
        {
            if (g.SRID is not -1 and not 0)
            {
                var transformer = _factory.Create(g.SRID, toSrid);
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
