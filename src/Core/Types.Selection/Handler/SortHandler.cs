using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Sorting;

namespace HotChocolate.Types.Selection
{
    public class SortHandler : IListHandler
    {
        private const string ARGUMENT_NAME =
            SortObjectFieldDescriptorExtensions.OrderByArgumentName;

        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            if (context.TryGetValueNode(ARGUMENT_NAME, out IValueNode sortArgument) &&
                selection.Field.Arguments[ARGUMENT_NAME].Type is InputObjectType iot &&
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
