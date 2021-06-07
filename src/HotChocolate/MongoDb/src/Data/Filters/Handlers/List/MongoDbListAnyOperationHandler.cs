using System;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;

namespace HotChocolate.Data.MongoDb.Filters
{
    /// <summary>
    /// This filter operation handler maps a Any operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class MongoDbListAnyOperationHandler
        : MongoDbOperationHandlerBase
    {
        public MongoDbListAnyOperationHandler()
        {
            CanBeNull = false;
        }

        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IListFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id is DefaultFilterOperations.Any;
        }

        /// <inheritdoc />
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

                return new OrMongoDbFilterDefinition(
                    new MongoDbFilterOperation(
                        path,
                        new BsonDocument
                        {
                            { "$exists", true },
                            { "$in", new BsonArray { new BsonArray(), BsonNull.Value } }
                        }),
                    new MongoDbFilterOperation(
                        path,
                        new BsonDocument { { "$exists", false } }));
            }

            throw new InvalidOperationException();
        }
    }
}
