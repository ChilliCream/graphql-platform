using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters
{
    public class MatchAnyQueryableFieldHandler
        : FilterFieldHandler<Expression, QueryableFilterContext>
    {
        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition) => true;
    }
}
