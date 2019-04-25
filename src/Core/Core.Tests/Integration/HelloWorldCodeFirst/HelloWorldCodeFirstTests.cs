using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Integration.HelloWorldCodeFirst
{
    public class HelloWorldCodeFirstTests
    {
        [Fact]
        public async Task ExecuteHelloWorldCodeFirstQuery()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterServiceProvider(CreateServiceProvider());
                c.RegisterQueryType<QueryHelloWorld>();
                c.RegisterMutationType<MutationHelloWorld>();
            });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ hello state }");

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteHelloWorldCodeFirstQueryWithArgument()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterServiceProvider(CreateServiceProvider());
                c.RegisterQueryType<QueryHelloWorld>();
                c.RegisterMutationType<MutationHelloWorld>();
            });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ hello(to: \"me\") state }");

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteHelloWorldCodeFirstClrQuery()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterServiceProvider(CreateServiceProvider());
                c.RegisterQueryType<QueryHelloWorldClr>();
            });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ hello state }");

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteHelloWorldCodeFirstClrQueryWithArgument()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterServiceProvider(CreateServiceProvider());
                c.RegisterQueryType<QueryHelloWorldClr>();
            });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ hello(to: \"me\") state }");

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteHelloWorldCodeFirstMutation()
        {
            // arrange
            var schema = Schema.Create(c =>
            {
                c.RegisterServiceProvider(CreateServiceProvider());
                c.RegisterQueryType<QueryHelloWorld>();
                c.RegisterMutationType<MutationHelloWorld>();
            });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "mutation { newState(state:\"1234567\") }");

            // assert
            Assert.Empty(result.Errors);
            result.MatchSnapshot();
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

            var serviceProvider = new Mock<IServiceProvider>(
                MockBehavior.Strict);

            serviceProvider.Setup(t => t.GetService(It.IsAny<Type>()))
                .Returns(serviceResolver);

            return serviceProvider.Object;
        }
    }
}
