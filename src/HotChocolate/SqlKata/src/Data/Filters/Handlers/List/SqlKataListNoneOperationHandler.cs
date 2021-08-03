using System;
using HotChocolate.Data.Filters;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// This filter operation handler maps a All operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class SqlKataListNoneOperationHandler : SqlKataListOperationHandlerBase
    {
        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.None;

        /// <inheritdoc />
        protected override Query HandleListOperation(
            SqlKataFilterVisitorContext context,
            IFilterField field,
            SqlKataFilterScope scope,
            string path)
        {
            throw new NotImplementedException();
            /*
            return new AndFilterDefinition(
                new SqlKataFilterOperation(
                    path,
                    new BsonDocument
                    {
                        { "$exists", true },
                        { "$nin", new BsonArray { new BsonArray(), BsonNull.Value } }
                    }),
                new SqlKataFilterOperation(
                    path,
                    new NotSqlKataFilterDefinition(
                        new SqlKataFilterOperation("$elemMatch", CombineOperationsOfScope(scope)))
                ));
        */
        }
    }
}
