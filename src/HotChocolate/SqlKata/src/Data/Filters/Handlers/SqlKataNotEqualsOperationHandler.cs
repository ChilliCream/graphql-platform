using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// This filter operation handler maps a NotEquals operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class SqlKataNotEqualsOperationHandler
        : SqlKataOperationHandlerBase
    {
        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id is DefaultFilterOperations.NotEquals;
        }

        /// <inheritdoc />
        public override Query HandleOperation(
            SqlKataFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            var column = context.GetSqlKataFilterScope().GetColumnName();
            if (context.RuntimeTypes.Peek().IsNullable)
            {
                if (parsedValue is null)
                {
                    return context.GetInstance().WhereNotNull(column);
                }

                return context.GetInstance().WhereNot(column, parsedValue).OrWhereNull(column);
            }
            return context.GetInstance().WhereNot(column, parsedValue);
        }
    }
}
