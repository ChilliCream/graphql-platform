using System;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbComparableGreaterThanOrEqualsHandler
        : MongoDbComparableOperationHandler
    {
        public MongoDbComparableGreaterThanOrEqualsHandler()
        {
            CanBeNull = false;
        }

        protected override int Operation => DefaultOperations.GreaterThanOrEquals;

        public override FilterDefinition<BsonDocument> HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (parsedValue is {})
            {
                var doc = new BsonDocument(
                    "$gte",
                    BsonValue.Create(parsedValue));

                return new BsonDocument(
                    context.GetMongoFilterScope().GetPath(),
                    doc);
            }

            throw new InvalidOperationException();
        }
    }
}
