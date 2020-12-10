using HotChocolate.Data.MongoDb.Execution;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb
{
    public static class MongoDbExecutableExtensions
    {
        /// <summary>
        /// Wraps a <see cref="IMongoCollection{TDocument}"/> with
        /// <see cref="MongoCollectionExecutable{T}"/> to help the execution engine to execute it
        /// more efficiently
        /// </summary>
        /// <param name="collection">The source of the <see cref="IExecutable"/></param>
        /// <typeparam name="T">The type parameter</typeparam>
        /// <returns>The wrapped object</returns>
        public static MongoCollectionExecutable<T> AsExecutable<T>(
            this IMongoCollection<T> collection)
        {
            return new MongoCollectionExecutable<T>(collection);
        }

        /// <summary>
        /// Wraps a <see cref="IAggregateFluent{TResult}"/> with
        /// <see cref="MongoAggregateFluentExecutable{T}"/> to help the execution engine to execute it
        /// more efficiently
        /// </summary>
        /// <param name="aggregate">The source of the <see cref="IExecutable"/></param>
        /// <typeparam name="T">The type parameter</typeparam>
        /// <returns>The wrapped object</returns>
        public static MongoAggregateFluentExecutable<T> AsExecutable<T>(
            this IAggregateFluent<T> aggregate)
        {
            return new MongoAggregateFluentExecutable<T>(aggregate);
        }

        /// <summary>
        /// Wraps a <see cref="IFindFluent{TDocument,TProjection}"/> with
        /// <see cref="MongoFindFluentExecutable{T}"/> to help the execution engine to execute it
        /// more efficiently
        /// </summary>
        /// <param name="collection">The source of the <see cref="IExecutable"/></param>
        /// <typeparam name="T">The type parameter</typeparam>
        /// <returns>The wrapped object</returns>
        public static MongoFindFluentExecutable<T> AsExecutable<T>(
            this IFindFluent<T, T> collection)
        {
            return new MongoFindFluentExecutable<T>(collection);
        }
    }
}
