using System.Linq.Expressions;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Selections
{
    public interface IListHandler
    {
        Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression);
    }
}
