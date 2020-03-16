using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;
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
            if (context.TryGetValueNode("where", out IValueNode filter) &&
                selection.Field.Arguments["where"].Type is InputObjectType iot &&
                iot is IFilterInputType fit)
            {
                var visitor = new QueryableFilterVisitor(
                    iot, fit.EntityType, context.Conversion);

                filter.Accept(visitor);

                return Expression.Call(
                    typeof(Enumerable),
                    "Where",
                    new[] { fit.EntityType },
                    expression,
                    visitor.CreateFilter());
            }

            return expression;
        }
    }
}
