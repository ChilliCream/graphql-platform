using HotChocolate.Data.Filters;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    public class MultiLineStringFilterType
        : GeometryFilterType<MultiLineString>
    {
    }
}
