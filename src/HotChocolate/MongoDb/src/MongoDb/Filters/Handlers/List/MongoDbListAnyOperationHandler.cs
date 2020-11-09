using System;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbListAnyOperationHandler
        : MongoDbOperationHandlerBase
    {
        public MongoDbListAnyOperationHandler()
        {
            CanBeNull = false;
        }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IListFilterInput &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id is DefaultOperations.Any;
        }

        public override MongoDbFilterDefinition HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            if (context.RuntimeTypes.Count > 0 &&
                context.RuntimeTypes.Peek().TypeArguments is { Count: > 0 } &&
                parsedValue is bool parsedBool &&
                context.Scopes.Peek() is MongoDbFilterScope scope)
            {
                var path = scope.GetPath();

                if (parsedBool)
                {
                    return new MongoDbFilterOperation(
                        path,
                        new BsonDocument
                        {
                            { "$exists", true },
                            { "$nin", new BsonArray { new BsonArray(), BsonNull.Value } }
                        });
                }

                return new MongoDbFilterOperation(
                    path,
                    new BsonDocument
                    {
                        { "$exists", true },
                        { "$ne", BsonNull.Value },
                        { "$eq", new BsonArray() }
                    });
            }

            throw new InvalidOperationException();
        }
    }
}
