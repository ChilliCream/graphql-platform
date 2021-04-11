using System.Collections.Generic;
using HotChocolate.Data.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters
{
    /// <summary>
    /// This filter operation handler maps a All operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class MongoDbListAllOperationHandler : MongoDbListOperationHandlerBase
    {
        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.All;

        /// <inheritdoc />
        protected override MongoDbFilterDefinition HandleListOperation(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            MongoDbFilterScope scope,
            string path)
        {
            var negatedChilds = new List<MongoDbFilterDefinition>();
            Queue<MongoDbFilterDefinition> level = scope.Level.Peek();

            while (level.Count > 0)
            {
                negatedChilds.Add(
                    new MongoDbFilterOperation(
                        path,
                        new MongoDbFilterOperation(
                            "$elemMatch",
                            new NotMongoDbFilterDefinition(level.Dequeue()))));
            }

            return new AndFilterDefinition(
                new MongoDbFilterOperation(
                    path,
                    new BsonDocument
                    {
                        { "$exists", true },
                        { "$nin", new BsonArray { new BsonArray(), BsonNull.Value } }
                    }),
                new NotMongoDbFilterDefinition(
                    new OrMongoDbFilterDefinition(negatedChilds)
                ));
        }
    }
}
