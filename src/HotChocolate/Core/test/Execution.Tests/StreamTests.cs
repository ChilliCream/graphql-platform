using System.Text;
using System.Threading.Tasks;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class StreamTests
    {
        [Fact]
        public async Task Stream_Nodes()
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
                                        nodes @stream(initialCount: 1) {
                                            id
                                            name
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

        [Fact]
        public async Task Do_Not_Stream_Nodes()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWarsTypes()
                    .ExecuteRequestAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"query($stream: Boolean) {
                                    hero(episode: NEW_HOPE) {
                                        id
                                        ... @defer(label: ""friends"") {
                                            friends {
                                                nodes @stream(initialCount: 1, if: $stream) {
                                                    id
                                                    name
                                                }
                                            }
                                        }
                                    }
                                }")
                            .SetVariableValue("stream", false)
                            .Create());

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
        public async Task Stream_Nested_Nodes()
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
                                        nodes @stream(initialCount: 1) {
                                            id
                                            name
                                            friends {
                                                nodes @stream(initialCount: 1) {
                                                    id
                                                    name
                                                }
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
