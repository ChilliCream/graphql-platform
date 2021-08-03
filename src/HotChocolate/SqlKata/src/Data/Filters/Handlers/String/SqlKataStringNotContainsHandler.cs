using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    public class SqlKataStringNotContainsHandler
        : SqlKataStringOperationHandler
    {
        public SqlKataStringNotContainsHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.NotContains;

        public override Query HandleOperation(
            SqlKataFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is string str)
            {
                var column = context.GetMongoFilterScope().GetPath();
                Query? query = context.GetInstance().WhereNotContains(column, str);
                if (context.RuntimeTypes.Peek().IsNullable)
                {
                    query = query.Or().WhereNull(column);
                }

                return query;
            }

            throw new InvalidOperationException();
        }
    }
}
