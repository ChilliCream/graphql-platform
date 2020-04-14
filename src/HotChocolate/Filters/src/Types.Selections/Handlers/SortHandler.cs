using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Sorting;
using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types.Selections.Handlers
{
    public class SortHandler : IListHandler
    {
        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            ISortingConvention convention = context.SelectionContext.SortingConvention;
            string argumentName = convention.GetArgumentName();
            if (context.TryGetValueNode(argumentName, out IValueNode? sortArgument) &&
                selection.Field.Arguments[argumentName].Type is InputObjectType iot &&
                iot is ISortInputType fit)
            {
                var visitorContext = new QueryableSortVisitorContext(
                    iot, fit.EntityType, false, convention);
                QueryableSortVisitor.Default.Visit(sortArgument, visitorContext);

                return visitorContext.Compile(expression);
            }

            return expression;
        }
    }
}
