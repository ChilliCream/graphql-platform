using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using Moq;
using Xunit;

namespace HotChocolate.Integration.HelloWorldCodeFirst
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
            IExecutionResult result = schema.Execute(
                "{ hello state }");

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void ExecuteHelloWorldCodeFirstQueryWithArgument()
        {
            // arrange
            Schema schema = Schema.Create(c =>
            {
                c.RegisterServiceProvider(CreateServiceProvider());
                c.RegisterQueryType<QueryHelloWorld>();
                c.RegisterMutationType<MutationHelloWorld>();
            });

            // act
            IExecutionResult result = schema.Execute(
                "{ hello(to: \"me\") state }");

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void ExecuteHelloWorldCodeFirstClrQuery()
        {
            // arrange
            Schema schema = Schema.Create(c =>
            {
                c.RegisterServiceProvider(CreateServiceProvider());
                c.RegisterQueryType<QueryHelloWorldClr>();
            });

            // act
            IExecutionResult result = schema.Execute(
                "{ hello state }");

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void ExecuteHelloWorldCodeFirstClrQueryWithArgument()
        {
            // arrange
            Schema schema = Schema.Create(c =>
            {
                c.RegisterServiceProvider(CreateServiceProvider());
                c.RegisterQueryType<QueryHelloWorldClr>();
            });

            // act
            IExecutionResult result = schema.Execute(
                "{ hello(to: \"me\") state }");

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
            IExecutionResult result = schema.Execute(
                "mutation { newState(state:\"1234567\") }");

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
}
