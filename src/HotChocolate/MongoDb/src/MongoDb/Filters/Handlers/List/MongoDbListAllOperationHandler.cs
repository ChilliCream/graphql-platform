using System;
using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbListAllOperationHandler : MongoDbListOperationHandlerBase
    {
        protected override int Operation => DefaultOperations.All;

        protected override FilterDefinition<BsonDocument> HandleListOperation(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            Type closureType,
            MongoDbFilterScope scope,
            string path,
            BsonDocument? bsonDocument) =>
            field.Type is IComparableOperationFilterInput
                ? CreateArrayAllScalar(scope, path)
                : CreateArrayAll(scope, path);

        private static BsonDocument CreateArrayAll(
            MongoDbFilterScope scope,
            string path)
        {
            var negatedChilds = new BsonArray();
            Queue<FilterDefinition<BsonDocument>> level = scope.Level.Peek();
            while (level.Count > 0)
            {
                negatedChilds.Add(level.Dequeue().DefaultRender());
            }

            return new BsonDocument(
                path,
                new BsonDocument(
                    "$not",
                    new BsonDocument(
                        "$elemMatch",
                        new BsonDocument("$nor", negatedChilds))));
        }

        private static BsonDocument CreateArrayAllScalar(
            MongoDbFilterScope scope,
            string path)
        {
            var negatedChilds = new BsonArray();
            Queue<FilterDefinition<BsonDocument>> level = scope.Level.Peek();
            while (level.Count > 0)
            {
                negatedChilds.Add(
                    new BsonDocument(
                        path,
                        new BsonDocument("$not", level.Dequeue().DefaultRender())));
            }

            return new BsonDocument("$nor", negatedChilds);
        }
    }
}
