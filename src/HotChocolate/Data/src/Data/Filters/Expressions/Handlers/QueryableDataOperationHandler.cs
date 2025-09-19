using System.Linq.Expressions;
using HotChocolate.Configuration;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableDataOperationHandler
    : FilterFieldHandler<QueryableFilterContext, Expression>
{
    protected virtual int Operation => DefaultFilterOperations.Data;

    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeConfiguration,
        IFilterFieldConfiguration fieldConfiguration)
    {
        return fieldConfiguration is FilterOperationFieldConfiguration def
            && def.Id == Operation;
    }
}
