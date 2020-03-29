using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Resolvers;
using HotChocolate.Types.Filters;

namespace HotChocolate.Types.Selections.Handlers
{
    public class FilterHandler : IListHandler
    {
        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            var argumentName = context.SelectionContext.FilterArgumentName;
            if (context.TryGetValueNode(argumentName, out IValueNode? filter) &&
                selection.Field.Arguments[argumentName].Type is InputObjectType iot &&
                iot is IFilterInputType fit)
            {
                var visitorContext = new QueryableFilterVisitorContext(
                    iot, fit.EntityType, context.Conversion, false);

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
