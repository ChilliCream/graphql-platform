using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using Xunit;
using static HotChocolate.Execution.QueryResultBuilder;

namespace HotChocolate.Execution;

public class DeferTests
{
    [Fact]
    public async Task NoOptimization_Defer_Single_Scalar_Field()
    {
        var result =
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

        var stream = Assert.IsType<ResponseStream>(result);

        stream.MatchSnapshot();
    }

    [Fact]
    public async Task NoOptimization_Defer_Only_Root()
    {
        var result =
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

        var stream = Assert.IsType<ResponseStream>(result);

        stream.MatchSnapshot();
    }

    [Fact]
    public async Task NoOptimization_Defer_One_Root()
    {
        var result =
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

        var stream = Assert.IsType<ResponseStream>(result);

        stream.MatchSnapshot();
    }

    [Fact]
    public async Task NoOptimization_Nested_Defer()
    {
        var result =
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

        var stream = Assert.IsType<ResponseStream>(result);

        stream.MatchSnapshot();
    }

    [Fact]
    public async Task NoOptimization_Spread_Defer()
    {
        var result =
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

        var stream = Assert.IsType<ResponseStream>(result);

        stream.MatchSnapshot();
    }

    [Fact]
    public async Task Do_Not_Defer()
    {
        var result =
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

        var queryResult = Assert.IsType<QueryResult>(result);

        queryResult.MatchSnapshot();
    }

    [Fact]
    public async Task Do_Not_Defer_If_Variable_Set_To_False()
    {
        var request = QueryRequestBuilder.New()
            .SetQuery(
                @"query($if: Boolean!) {
                    hero(episode: NEW_HOPE) {
                        id
                        ... deferred @defer(label: ""friends"", if: $if)
                    }
                }

                fragment deferred on Character {
                    friends {
                        nodes {
                            id
                        }
                    }
                }")
            .SetVariableValue("if", false)
            .Create();

        var result =
            await new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .AddStarWarsTypes()
                .ExecuteRequestAsync(request);

        var queryResult = Assert.IsType<QueryResult>(result);

        queryResult.MatchSnapshot();
    }

    [Fact]
    public async Task Do_Defer_If_Variable_Set_To_True()
    {
        var request = QueryRequestBuilder.New()
            .SetQuery(
                @"query($if: Boolean!) {
                    hero(episode: NEW_HOPE) {
                        id
                        ... deferred @defer(label: ""friends"", if: $if)
                    }
                }

                fragment deferred on Character {
                    friends {
                        nodes {
                            id
                        }
                    }
                }")
            .SetVariableValue("if", true)
            .Create();

        var result =
            await new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .AddStarWarsTypes()
                .ExecuteRequestAsync(request);

        var stream = Assert.IsType<ResponseStream>(result);

        stream.MatchSnapshot();
    }
}
