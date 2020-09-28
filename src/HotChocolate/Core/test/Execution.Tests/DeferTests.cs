using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Serialization;
using HotChocolate.StarWars;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class DeferTests
    {
        [Fact]
        public async Task NoOptimization_Defer_Single_Scalar_Field()
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

            IResponseStream stream = Assert.IsType<DeferredQueryResult>(result);

            var results = new StringBuilder();

            await foreach (IQueryResult payload in stream.ReadResultsAsync())
            {
                results.AppendLine(payload.ToJson());
                results.AppendLine();
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task NoOptimization_Nested_Defer()
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
                                ... @defer(label: ""friends"") {
                                    friends {
                                        nodes {
                                            id
                                            ... @defer {
                                                name
                                            }
                                        }
                                    }
                                }
                            }
                        }");

            IResponseStream stream = Assert.IsType<DeferredQueryResult>(result);

            var results = new StringBuilder();

            await foreach (IQueryResult payload in stream.ReadResultsAsync())
            {
                results.AppendLine(payload.ToJson());
                results.AppendLine();
            }

            results.ToString().MatchSnapshot();
        }
    }
}
