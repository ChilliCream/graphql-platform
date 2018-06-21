using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Integration
{
    public class HelloWorldCodeFirstTests
    {
        [Fact]
        public void ExecuteHelloWorldCodeFirstQuery()
        {
            // arrange
            Schema schema = Schema.Create(c =>
            {
                c.RegisterServiceProvider(CreateServiceProvider());
                c.RegisterQueryType<QueryHelloWorld>();
                c.RegisterMutationType<MutationHelloWorld>();
            });

            // act
            QueryResult result = schema.Execute(
                "mutation { newState(state:\"1234567\") }");

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void ExecuteHelloWorldCodeFirstMutation()
        {
            // arrange
            Schema schema = Schema.Create(c =>
            {
                c.RegisterServiceProvider(CreateServiceProvider());
                c.RegisterQueryType<QueryHelloWorld>();
                c.RegisterMutationType<MutationHelloWorld>();
            });

            // act
            QueryResult result = schema.Execute(
                "{ hello state }");

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        private IServiceProvider CreateServiceProvider()
        {
            Dictionary<Type, object> services = new Dictionary<Type, object>();
            services[typeof(DataStoreHelloWorld)] = new DataStoreHelloWorld();

            Func<Type, object> serviceResolver = new Func<Type, object>(
                t =>
                {
                    if (services.TryGetValue(t, out object s))
                    {
                        return s;
                    }
                    return null;
                });

            Mock<IServiceProvider> serviceProvider =
                new Mock<IServiceProvider>(MockBehavior.Strict);
            serviceProvider.Setup(t => t.GetService(It.IsAny<Type>()))
                .Returns(serviceResolver);
            return serviceProvider.Object;
        }
    }

    public class QueryHelloWorld
        : ObjectType
    {
        public QueryHelloWorld(DataStoreHelloWorld dataStore)
        {
            DataStore = dataStore;
        }

        public DataStoreHelloWorld DataStore { get; }

        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("hello").Resolver(() => "world");
            descriptor.Field("state").Resolver(() => DataStore.State);
        }
    }

    public class MutationHelloWorld
        : ObjectType
    {
        public MutationHelloWorld(DataStoreHelloWorld dataStore)
        {
            DataStore = dataStore;
        }

        public DataStoreHelloWorld DataStore { get; }

        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("newState")
                .Argument("state", a => a.Type<StringType>())
                .Resolver(c => DataStore.State = c.Argument<string>("state"));
        }
    }

    public class DataStoreHelloWorld
    {
        public string State { get; set; } = "initial";
    }
}
