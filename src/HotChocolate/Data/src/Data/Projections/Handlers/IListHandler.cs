using System.Linq.Expressions;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Handlers
{
    public interface IListHandler
    {
        Expression HandleLeave(
            SelectionVisitorContext context,
            IFieldSelection selection,
            Expression expression);
    }
}
