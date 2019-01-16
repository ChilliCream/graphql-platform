using System;
using System.Collections.Generic;
using Moq;

namespace HotChocolate.Benchmark.Tests.Execution
{
    public static class SchemaFactory
    {
        public static Schema Create()
        {
            var repository = new CharacterRepository();
            var services = new Dictionary<Type, object>();
            services[typeof(CharacterRepository)] = repository;
            services[typeof(Query)] = new Query(repository);
            services[typeof(Mutation)] = new Mutation();


            var serviceResolver = new Func<Type, object>(
                t =>
                {
                    if (services.TryGetValue(t, out object s))
                    {
                        return s;
                    }
                    return null;
                });

            var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

            serviceProvider.Setup(t => t.GetService(It.IsAny<Type>()))
                .Returns(serviceResolver);

            return Schema.Create(c =>
            {
                c.RegisterServiceProvider(serviceProvider.Object);
                c.RegisterQueryType<QueryType>();
                c.RegisterMutationType<MutationType>();
                c.RegisterType<HumanType>();
                c.RegisterType<DroidType>();
                c.RegisterType<EpisodeType>();
            });
        }
    }
}
