using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using HotChocolate.Execution;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.Stitching
{
    public class SchemaCreationTests
       : StitchingTestBase
    {
        public SchemaCreationTests(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [InlineData(SchemaCreation.OnFirstRequest, "LazyQueryExecutor")]
        [InlineData(SchemaCreation.OnStartup, "QueryExecutor")]
        [Theory]
        public void QueryExecutorOptions(
            SchemaCreation creation,
            string executorName)
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);

            // act
            serviceCollection.AddStitchedSchema(b => b
                .AddSchemaFromHttp("contract")
                .SetSchemaCreation(creation));

            // assert
            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            Assert.Equal(executorName, executor.GetType().Name);
        }
    }
}
