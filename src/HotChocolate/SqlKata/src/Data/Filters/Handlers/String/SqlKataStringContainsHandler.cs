using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    public class SqlKataStringContainsHandler
        : SqlKataStringOperationHandler
    {
        public SqlKataStringContainsHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.Contains;

        public override Query HandleOperation(
            SqlKataFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is string str)
            {
                var column = context.GetSqlKataFilterScope().GetColumnName();
                return context.GetInstance().WhereContains(column, str);
            }

            throw new InvalidOperationException();
        }
    }
}
