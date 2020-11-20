using HotChocolate.Configuration;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableEnumNotEqualsHandler
        : QueryableComparableNotEqualsHandler
    {
        public QueryableEnumNotEqualsHandler(
            ITypeConverter typeConverter)
            : base(typeConverter)
        {
        }

        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IEnumOperationFilterInput &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }
    }
}
