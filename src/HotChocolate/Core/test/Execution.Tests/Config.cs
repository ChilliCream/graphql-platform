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
        public async void Something(IServiceCollection services)
        {
            services.AddGraphQL("foo")
                .AddQueryType<Query>()
                .ConfigureSchemaAsync((b, ct) => 
                {
                    return default;
                });

            IRequestExecutorResolver resolver = services.BuildServiceProvider().GetRequiredService<IRequestExecutorResolver>();
            IRequestExecutor executor = await resolver.GetRequestExecutorAsync("foo");
            IExecutionResult result = await executor.ExecuteAsync(QueryRequestBuilder.New().SetQuery("{ __typename }").Create());
        }
    }

    public class Query
    {

    }
}