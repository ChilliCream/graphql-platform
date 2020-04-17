using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Selections.Handlers
{
    public class FilterHandler : IListHandler
    {
        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            IFilterConvention convention = context.SelectionContext.FilterConvention;
            NameString argumentName = convention.GetArgumentName();
            if (context.TryGetValueNode(argumentName, out IValueNode? filter) &&
                selection.Field.Arguments[argumentName].Type is InputObjectType iot &&
                iot is IFilterInputType fit && 
                convention.TryGetVisitorDefinition(
                    out FilterExpressionVisitorDefinition? defintion))
            {
                var visitorContext = new QueryableFilterVisitorContext(
                    iot,
                    fit.EntityType,
                    defintion,
                    context.Conversion,
                    false);

                QueryableFilterVisitor.Default.Visit(filter, visitorContext);

                return Expression.Call(
                    typeof(Enumerable),
                    "Where",
                    new[] { fit.EntityType },
                    expression,
                    visitorContext.CreateFilter());
            }

            return expression;
        }
    }
}
