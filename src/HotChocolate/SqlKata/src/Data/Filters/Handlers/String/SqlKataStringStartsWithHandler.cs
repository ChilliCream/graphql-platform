using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    public class SqlKataStringStartsWithHandler
        : SqlKataStringOperationHandler
    {
        public SqlKataStringStartsWithHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultFilterOperations.StartsWith;

        public override Query HandleOperation(
            SqlKataFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is string str)
            {
                var column = context.GetMongoFilterScope().GetPath();
                return context.GetInstance().WhereStarts(column, str);
            }

            throw new InvalidOperationException();
        }
    }
}
