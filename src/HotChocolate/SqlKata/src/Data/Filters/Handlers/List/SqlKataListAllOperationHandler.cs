using System;
using System.Collections.Generic;
using HotChocolate.Data.Filters;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// This filter operation handler maps a All operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class SqlKataListAllOperationHandler : SqlKataListOperationHandlerBase
    {
        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.All;

        /// <inheritdoc />
        protected override Query HandleListOperation(
            SqlKataFilterVisitorContext context,
            IFilterField field,
            SqlKataFilterScope scope,
            string path)
        {
            /*
            var negatedChilds = new List<Query>();
            Queue<Query> level = scope.Level.Peek();

            context.GetInstance().Where

            while (level.Count > 0)
            {
                negatedChilds.Add(
                    new SqlKataFilterOperation(
                        path,
                        new SqlKataFilterOperation(
                            "$elemMatch",
                            new NotSqlKataFilterDefinition(level.Dequeue()))));
            }

            return new AndFilterDefinition(
                new SqlKataFilterOperation(
                    path,
                    new BsonDocument
                    {
                        { "$exists", true },
                        { "$nin", new BsonArray { new BsonArray(), BsonNull.Value } }
                    }),
                new NotSqlKataFilterDefinition(
                    new OrSqlKataFilterDefinition(negatedChilds)
                ));
        */
            throw new NotImplementedException();
        }
    }
}
