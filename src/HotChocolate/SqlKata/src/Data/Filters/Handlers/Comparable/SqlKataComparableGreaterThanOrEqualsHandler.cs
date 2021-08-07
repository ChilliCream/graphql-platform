using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// This filter operation handler maps a GreaterThanOrEquals operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class SqlKataComparableGreaterThanOrEqualsHandler
        : SqlKataComparableOperationHandler
    {
        public SqlKataComparableGreaterThanOrEqualsHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.GreaterThanOrEquals;

        /// <inheritdoc />
        public override Query HandleOperation(
            SqlKataFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is {})
            {
                var column = context.GetSqlKataFilterScope().GetColumnName();
                return context.GetInstance().Where(column, ">=", parsedValue);
            }

            throw new InvalidOperationException();
        }
    }
}
