using System;
using System.Collections.Generic;
using ChilliCream.Testing;
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
            result.Snapshot();
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
            result.Snapshot();
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
            result.Snapshot();
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
            result.Snapshot();
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
            result.Snapshot();
        }

        private IServiceProvider CreateServiceProvider()
        {
            var services = new Dictionary<Type, object>();
            services[typeof(DataStoreHelloWorld)] = new DataStoreHelloWorld();
            services[typeof(QueryHelloWorldClr)] =
                new QueryHelloWorldClr(new DataStoreHelloWorld());

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
            return serviceProvider.Object;
        }
    }
}
