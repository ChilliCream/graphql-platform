using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// This filter operation handler maps a In operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class SqlKataInOperationHandler
        : SqlKataInOperationHandlerBase
    {
        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id is DefaultFilterOperations.In;
        }

        /// <inheritdoc />
        public override Query HandleOperation(
            SqlKataFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            var column = context.GetMongoFilterScope().GetPath();
            if (parsedValue is IList list)
            {
                if (context.RuntimeTypes.Peek().IsNullable)
                {
                    var (hasNull, values) = ExtractValues(list);

                    Query query = context
                        .GetInstance()
                        .WhereIn(column, values);

                    if (hasNull)
                    {
                        return query.Or().WhereNull(column);
                    }

                    return query.WhereNotNull(column);
                }

                return context.GetInstance().WhereIn(column, list.Cast<object>());
            }

            throw new InvalidOperationException();
        }
    }
}
