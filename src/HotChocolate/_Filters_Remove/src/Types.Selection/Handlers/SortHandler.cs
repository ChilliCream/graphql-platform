using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Sorting;

namespace HotChocolate.Types.Selections.Handlers
{
    public class SortHandler : IListHandler
    {
        private const string ArgumentName =
            SortObjectFieldDescriptorExtensions.OrderByArgumentName;

        public Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression)
        {
            if (context.TryGetValueNode(ArgumentName, out IValueNode sortArgument) &&
                selection.Field.Arguments[ArgumentName].Type is InputObjectType iot &&
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
