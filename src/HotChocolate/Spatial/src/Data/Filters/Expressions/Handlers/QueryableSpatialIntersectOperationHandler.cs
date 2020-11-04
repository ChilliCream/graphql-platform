using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using NetTopologySuite.Geometries;

namespace HotChocolate.Data.Filters.Spatial
{
    public class QueryableSpatialIntersectsOperationHandler
        : QueryableSpatialMethodHandler
    {
        private static readonly MethodInfo _intersects =
            typeof(Geometry).GetMethod(nameof(Geometry.Intersects))!;

        public QueryableSpatialIntersectsOperationHandler(
            IFilterConvention convention,
            ITypeInspector inspector)
            : base(convention, inspector, _intersects)
        {
        }

        protected override int Operation => SpatialFilterOperations.Intersects;

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
                    result = ExpressionBuilder.Intersects(
                        context.GetInstance(),
                        ExpressionBuilder.Buffer(g, buffer));

                    return true;
                }

                result = ExpressionBuilder.Intersects(context.GetInstance(), g);
                return true;
            }

            result = null;
            return false;
        }
    }
}
