using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbComparableNotLowerThanHandler
        : MongoDbComparableOperationHandler
    {
        public MongoDbComparableNotLowerThanHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultOperations.NotLowerThan;

        public override FilterDefinition<BsonDocument> HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is {})
            {
                var doc = new BsonDocument(
                    "$not",
                    new BsonDocument("$lt", BsonValue.Create(parsedValue)));

                return new BsonDocument(
                    context.GetMongoFilterScope().GetPath(),
                    doc);
            }

            throw new InvalidOperationException();
        }
    }
}
