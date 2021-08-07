using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// This filter operation handler maps a NotLowerThan operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class SqlKataComparableNotLowerThanHandler
        : SqlKataComparableOperationHandler
    {
        public SqlKataComparableNotLowerThanHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.NotLowerThan;

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
                Query? query =  context.GetInstance().WhereNot(column, "<", parsedValue);
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
