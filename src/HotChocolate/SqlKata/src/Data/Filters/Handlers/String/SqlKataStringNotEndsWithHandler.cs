using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    public class SqlKataStringNotEndsWithHandler
        : SqlKataStringOperationHandler
    {
        public SqlKataStringNotEndsWithHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.NotEndsWith;

        public override Query HandleOperation(
            SqlKataFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is string str)
            {
                var column = context.GetSqlKataFilterScope().GetColumnName();
                Query? query = context.GetInstance().WhereNotEnds(column, str);
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
