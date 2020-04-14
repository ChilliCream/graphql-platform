using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.StarWars;
using Xunit;

namespace HotChocolate.Tests
{
    public static class TestHelper
    {
        public static Task<IExecutionResult> ExpectValid(string query)
        {
            return ExpectValid(new TestConfiguration(), query);
        }

        public static Task<IExecutionResult> ExpectValid(ISchema schema, string query)
        {
            return ExpectValid(new TestConfiguration { Schema = schema }, query);
        }

        public static async Task<IExecutionResult> ExpectValid(
            TestConfiguration? configuration,
            string query)
        {
            // arrange
            configuration = new TestConfiguration();
            configuration.Schema ??= SchemaBuilder.New().AddStarWarsTypes().Create();
            configuration.Executor ??= configuration.Schema.MakeExecutable();
            configuration.Service ??=
                new ServiceCollection().AddStarWarsRepositories().BuildServiceProvider();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetServices(configuration.Service)
                    .Create();

            // act
            IExecutionResult result = await configuration.Executor.ExecuteAsync(request, default);

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
            ISchema schema,
            string query,
            params Action<IError>[] elementInspectors)
        {
            return ExpectError(new TestConfiguration { Schema = schema }, query, elementInspectors);
        }

        public static async Task ExpectError(
            TestConfiguration? configuration,
            string query,
            params Action<IError>[] elementInspectors)
        {
            // arrange
            configuration = new TestConfiguration();
            configuration.Schema ??= SchemaBuilder.New().AddStarWarsTypes().Create();
            configuration.Executor ??= configuration.Schema.MakeExecutable();
            configuration.Service ??=
                new ServiceCollection().AddStarWarsRepositories().BuildServiceProvider();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetServices(configuration.Service)
                    .Create();

            // act
            IExecutionResult result = await configuration.Executor.ExecuteAsync(request, default);

            // assert
            Assert.NotNull(result.Errors);

            if (elementInspectors.Length > 0)
            {
                Assert.Collection(result.Errors, elementInspectors);
            }

            result.MatchSnapshot();
        }
    }
}
