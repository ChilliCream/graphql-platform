using System;
using HotChocolate.Data.Filters;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// This filter operation handler maps a Some operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class SqlKataListSomeOperationHandler : SqlKataListOperationHandlerBase
    {
        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.Some;

        /// <inheritdoc />
        protected override Query HandleListOperation(
            SqlKataFilterVisitorContext context,
            IFilterField field,
            SqlKataFilterScope scope,
            string path)
        {
            throw new NotImplementedException();
            /*
            return new SqlKataFilterOperation(
                path,
                new SqlKataFilterOperation("$elemMatch", CombineOperationsOfScope(scope)));
        */
        }
    }
}
