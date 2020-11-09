using System;
using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Language;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbListAllOperationHandler : MongoDbListOperationHandlerBase
    {
        protected override int Operation => DefaultOperations.All;

        protected override MongoDbFilterDefinition HandleListOperation(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            ObjectFieldNode node,
            Type closureType,
            MongoDbFilterScope scope,
            string path,
            MongoDbFilterDefinition? bsonDocument) =>
            field.Type is IComparableOperationFilterInput
                ? CreateArrayAllScalar(scope, path)
                : CreateArrayAll(scope, path);

        private static MongoDbFilterDefinition CreateArrayAll(
            MongoDbFilterScope scope,
            string path)
        {
            var negatedChilds = new List<MongoDbFilterDefinition>();
            Queue<MongoDbFilterDefinition> level = scope.Level.Peek();
            while (level.Count > 0)
            {
                negatedChilds.Add(level.Dequeue());
            }

            return new MongoDbFilterOperation(
                path,
                new NotMongoDbFilterDefinition(
                    new MongoDbFilterOperation(
                        "$elemMatch",
                        new NotMongoDbFilterDefinition(
                            new OrMongoDbFilterDefinition(negatedChilds)))));
        }

        private static MongoDbFilterDefinition CreateArrayAllScalar(
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
                        new NotMongoDbFilterDefinition(level.Dequeue())));
            }

            return new NotMongoDbFilterDefinition(
                new OrMongoDbFilterDefinition(negatedChilds));
        }
    }
}
