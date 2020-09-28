using System.Linq.Expressions;
using HotChocolate.Configuration;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableDataOperationHandler
        : FilterFieldHandler<QueryableFilterContext, Expression>
    {
        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition def &&
                def.Id == DefaultOperations.Data;
        }
    }
}
