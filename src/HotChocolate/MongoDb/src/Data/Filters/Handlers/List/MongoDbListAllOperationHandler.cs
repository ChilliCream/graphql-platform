using System.Collections.Generic;
using HotChocolate.Data.Filters;
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
            string path) =>
            field.Type is IComparableOperationFilterInputType
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
