using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Sorting;

namespace HotChocolate.Types.Selections.Handlers
{
    public class SortHandler : IListHandler
    {
        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            var argumentName = context.SelectionContext.SortingArgumentName;
            if (context.TryGetValueNode(argumentName, out IValueNode sortArgument) &&
                selection.Field.Arguments[argumentName].Type is InputObjectType iot &&
                iot is ISortInputType fit)
            {
                var visitor = new QueryableSortVisitor(iot, fit.EntityType);

                sortArgument.Accept(visitor);
                return visitor.Compile(expression);
            }

            return expression;
        }
    }
}
