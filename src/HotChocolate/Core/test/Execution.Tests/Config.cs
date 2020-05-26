using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.StarWars.Data;
using Moq;
using Xunit;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using ChilliCream.Testing;

namespace HotChocolate.Execution
{
    public class Foo
    {
        [Fact]
        public async void Something()
        {
            var services = new ServiceCollection();
            services.AddGraphQL("foo")
                .AddQueryType<Query>()
                .UseDefaultPipeline()
                .UseDocumentParser()
                .ConfigureSchemaAsync((b, ct) =>
                {
                    return default;
                });

            IRequestExecutorResolver resolver = services.BuildServiceProvider().GetRequiredService<IRequestExecutorResolver>();
            IRequestExecutor executor = await resolver.GetRequestExecutorAsync("foo");
            IExecutionResult result = await executor.ExecuteAsync(QueryRequestBuilder.New().SetQuery("{ __typename }").Create());

            result.ToJson().MatchSnapshot();
        }


        public class Query
        {
            public string Hello() => "World";
        }
    }
}