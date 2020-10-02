using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbListNoneOperationHandler : MongoDbListOperationHandlerBase
    {
        protected override int Operation => DefaultOperations.None;

        protected override FilterDefinition<BsonDocument> HandleListOperation(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            Type closureType,
            MongoDbFilterScope scope,
            string path,
            BsonDocument? bsonDocument)
        {
            return new BsonDocument(
                path,
                new BsonDocument(
                    "$not",
                    new BsonDocument(
                        "$elemMatch",
                        GetFilters(context, scope).DefaultRender())));
        }
    }
}
