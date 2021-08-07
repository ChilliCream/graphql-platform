using System;
using System.Collections;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// This filter operation handler maps a NotIn operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class SqlKataNotInOperationHandler
        : SqlKataInOperationHandlerBase
    {
        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id is DefaultFilterOperations.NotIn;
        }

        /// <inheritdoc />
        public override Query HandleOperation(
            SqlKataFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            var column = context.GetSqlKataFilterScope().GetColumnName();
            if (parsedValue is not IList list)
            {
                throw new InvalidOperationException();
            }

            var (hasNull, values) = ExtractValues(list);

            Query query = context
                .GetInstance()
                .WhereNotIn(column, values);

            if (context.RuntimeTypes.Peek().IsNullable)
            {
                if (hasNull)
                {
                    query = query.WhereNotNull(column);
                }
                else
                {
                    query = query.Or().WhereNull(column);
                }
            }

            return query;
        }
    }
}
