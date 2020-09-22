using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Spatial.Filters
{
    public class QueryableSpatialContainsOperationHandler
        : QueryableSpatialMethodHandler
    {
        public QueryableSpatialContainsOperationHandler(
            IFilterConvention convention) : base(convention)
        {
        }

        protected override int Operation => SpatialFilterOperations.Contains;

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
                    result = ExpressionBuilder.Contains(
                        context.GetInstance(),
                        ExpressionBuilder.Buffer(g, buffer));

                    return true;
                }

                result = ExpressionBuilder.Contains(context.GetInstance(), g);
                return true;
            }

            result = null;
            return false;
        }
    }
}
