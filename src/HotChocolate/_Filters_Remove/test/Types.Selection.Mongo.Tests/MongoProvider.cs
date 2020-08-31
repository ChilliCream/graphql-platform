using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Squadron;

namespace HotChocolate.Types.Selections
{
    public class MongoProvider : IResolverProvider
    {
        private readonly MongoResource _resource;

        public MongoProvider(MongoResource resource)
        {
            _resource = resource;
        }

        public (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            CreateResolver<TResult>(params TResult[] results)
            where TResult : class
        {
            var services = new ServiceCollection();
            IMongoDatabase database = _resource.CreateDatabase();
            IMongoCollection<TResult> collection = database.GetCollection<TResult>("col");
            collection.InsertMany(results);

            services.AddSingleton<IMongoCollection<TResult>>(collection);

            return (services, ctx => ctx.Service<IMongoCollection<TResult>>().AsQueryable());
        }

        public (IServiceCollection, Func<IResolverContext, IAsyncEnumerable<TResult>>)
            CreateAsyncResolver<TResult>(params TResult[] results)
            where TResult : class
        {
            var services = new ServiceCollection();
            IMongoDatabase database = _resource.CreateDatabase();
            IMongoCollection<TResult> collection = database.GetCollection<TResult>("col2");
            collection.InsertMany(results);

            services.AddSingleton<IMongoCollection<TResult>>(collection);

            return (services, ctx => ctx.Service<IMongoCollection<TResult>>()
                        .AsQueryable()
                        .ToAsyncEnumerable());
        }
    }
}
