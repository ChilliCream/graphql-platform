using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.StarWars;
using HotChocolate.Subscriptions;
using Xunit;

namespace HotChocolate.Tests
{
    public static class TestHelper
    {
        public static Task<IExecutionResult> ExpectValid(string query)
        {
            return ExpectValid(new TestConfiguration(), query);
        }

        public static Task<IExecutionResult> ExpectValid(
            string query,
            Action<IQueryRequestBuilder> modifyRequest)
        {
            return ExpectValid(new TestConfiguration { ModifyRequest = modifyRequest }, query);
        }

        public static Task<IExecutionResult> ExpectValid(
            Action<ISchemaBuilder> createSchema,
            string query)
        {
            return ExpectValid(new TestConfiguration { CreateSchema = createSchema }, query);
        }

        public static async Task<IExecutionResult> ExpectValid(
            TestConfiguration? configuration,
            string query)
        {
            // arrange
            IQueryExecutor executor = CreateExecutor(configuration);
            IReadOnlyQueryRequest request = CreateRequest(configuration, query);

            // act
            IExecutionResult result = await executor.ExecuteAsync(request, default);

            // assert
            Assert.Null(result.Errors);
            return result;
        }

        public static Task ExpectError(
            string query,
            params Action<IError>[] elementInspectors)
        {
            return ExpectError(new TestConfiguration(), query, elementInspectors);
        }

        public static Task ExpectError(
            string query,
            Action<IQueryRequestBuilder> modifyRequest,
            params Action<IError>[] elementInspectors)
        {
            return ExpectError(
                new TestConfiguration { ModifyRequest = modifyRequest },
                query,
                elementInspectors);
        }

        public static Task ExpectError(
            Action<ISchemaBuilder> createSchema,
            string query,
            params Action<IError>[] elementInspectors)
        {
            return ExpectError(
                new TestConfiguration { CreateSchema = createSchema },
                query,
                elementInspectors);
        }

        public static async Task ExpectError(
            TestConfiguration? configuration,
            string query,
            params Action<IError>[] elementInspectors)
        {
            // arrange
            IQueryExecutor executor = CreateExecutor(configuration);
            IReadOnlyQueryRequest request = CreateRequest(configuration, query);

            // act
            IExecutionResult result = await executor.ExecuteAsync(request, default);

            // assert
            Assert.NotNull(result.Errors);

            if (elementInspectors.Length > 0)
            {
                Assert.Collection(result.Errors, elementInspectors);
            }

            result.MatchSnapshot();
        }

        private static IQueryExecutor CreateExecutor(TestConfiguration? configuration)
        {
            configuration ??= new TestConfiguration();
            configuration.CreateSchema ??= b => b.AddStarWarsTypes();
            configuration.CreateExecutor ??= s => s.MakeExecutable();

            var builder = SchemaBuilder.New();
            configuration.CreateSchema(builder);
            return configuration.CreateExecutor(builder.Create());
        }

        private static IReadOnlyQueryRequest CreateRequest(
            TestConfiguration? configuration, string query)
        {
            configuration ??= new TestConfiguration();
            configuration.Service ??= new ServiceCollection()
                .AddStarWarsRepositories()
                .AddInMemorySubscriptionProvider()
                .BuildServiceProvider();

            IQueryRequestBuilder builder =
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetServices(configuration.Service);

            if (configuration.ModifyRequest is { })
            {
                configuration.ModifyRequest(builder);
            }

            return builder.Create();
        }
    }
}
