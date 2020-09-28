using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    public class QueryableSpatialTouchesOperationHandler
        : QueryableSpatialMethodHandler
    {
        private static readonly MethodInfo _touches =
            typeof(Geometry).GetMethod(nameof(Geometry.Touches))!;

        public QueryableSpatialTouchesOperationHandler(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector, _touches)
        {
        }

        protected override int Operation => SpatialFilterOperations.Touches;

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
