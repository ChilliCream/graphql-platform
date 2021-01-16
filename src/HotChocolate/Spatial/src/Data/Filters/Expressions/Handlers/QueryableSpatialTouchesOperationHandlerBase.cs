using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;
using static HotChocolate.Data.Filters.Spatial.SpatialOperationHandlerHelper;

namespace HotChocolate.Data.Filters.Spatial
{
    public abstract class QueryableSpatialTouchesOperationHandlerBase
        : QueryableSpatialBooleanMethodHandler
    {
        private static readonly MethodInfo _touches =
            typeof(Geometry).GetMethod(nameof(Geometry.Touches))!;

        public QueryableSpatialTouchesOperationHandlerBase(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector, _touches)
        {
        }

        protected override bool TryHandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out Expression? result)
        {
            if (TryGetParameter(field, node.Value, GeometryFieldName, out Geometry g))
            {
                if (TryGetParameter(field, node.Value, BufferFieldName, out double buffer))
                {
                    result = ExpressionBuilder.Touches(
                        context.GetInstance(),
                        ExpressionBuilder.Buffer(g, buffer));

                    return true;
                }

                result = ExpressionBuilder.Touches(context.GetInstance(), g);
                return true;
            }

            result = null;
            return false;
        }
    }
}
