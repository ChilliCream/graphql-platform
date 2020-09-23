using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class DeferTests
    {
        [Fact]
        public async Task Foo()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWarsTypes()
                    .ExecuteRequestAsync(
                        @"{
                            hero(episode: NEW_HOPE) {
                                id
                                ... @defer {
                                    name
                                }
                            }
                        }");

            IResponseStream stream = Assert.IsType<DeferredResult>(result);

            var results = new List<string>();
            await foreach (IQueryResult payload in stream.ReadResultsAsync())
            {
                results.Add(payload.ToJson());
            }
            results.MatchSnapshot();
        }

    }
}
