using System.Linq.Expressions;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Selection
{
    public interface IListHandler
    {
        Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression);
    }
}
