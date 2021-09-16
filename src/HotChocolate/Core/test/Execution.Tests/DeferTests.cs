using System.Text;
using System.Threading.Tasks;
using HotChocolate.StarWars;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
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
        public async Task NoOptimization_Defer_Only_Root()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWarsTypes()
                    .ExecuteRequestAsync(
                        @"{
                            ... @defer {
                                hero(episode: NEW_HOPE) {
                                    id
                                    name
                                }
                            }
                        }");

            Assert.IsType<QueryResult>(result).MatchSnapshot();
        }

        [Fact]
        public async Task NoOptimization_Defer_One_Root()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWarsTypes()
                    .ExecuteRequestAsync(
                        @"{
                            ... @defer {
                                a: hero(episode: NEW_HOPE) {
                                    id
                                    name
                                }
                            }
                            b: hero(episode: NEW_HOPE) {
                                id
                                name
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

        [Fact]
        public async Task NoOptimization_Spread_Defer()
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
                                ... deferred @defer(label: ""friends"")
                            }
                        }

                        fragment deferred on Character {
                            friends {
                                nodes {
                                    id
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

        [Fact(Skip = "needs to be fixed.")]
        public async Task Do_Not_Defer()
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
                                ... deferred @defer(label: ""friends"", if: false)
                            }
                        }

                        fragment deferred on Character {
                            friends {
                                nodes {
                                    id
                                }
                            }
                        }");

            Assert.IsType<QueryResult>(result).MatchSnapshot();
        }
    }
}
