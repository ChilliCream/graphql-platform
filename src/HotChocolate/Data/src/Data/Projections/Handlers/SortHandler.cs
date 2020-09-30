using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Handlers
{
    /*
    public class SortHandler : IListHandler
    {
        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            var argumentName = context.SelectionContext.SortingArgumentName;
            if (context.TryGetValueNode(argumentName, out IValueNode? sortArgument) &&
                selection.Field.Arguments[argumentName].Type is InputObjectType iot &&
                iot is ISortInputType fit)
            {
                var visitorContext = new QueryableSortVisitorContext(
                    iot, fit.EntityType, false);
                QueryableSortVisitor.Default.Visit(sortArgument, visitorContext);

                return visitorContext.Compile(expression);
            }

            return expression;
        }
    }
    */
}
