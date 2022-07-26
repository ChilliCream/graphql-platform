using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using HotChocolate.Tests;
using Snapshooter.Xunit;
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

        IResponseStream stream = Assert.IsType<ResponseStream>(result);

        var results = new StringBuilder();

        await foreach (var payload in stream.ReadResultsAsync())
        {
            results.AppendLine(payload.ToJson());
            results.AppendLine();
        }

        results.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task NoOptimization_Defer_Only_Root()
    {
        Snapshot.FullName();

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

        await Assert.IsType<ResponseStream>(result).MatchSnapshotAsync();
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

        IResponseStream stream = Assert.IsType<ResponseStream>(result);

        var results = new StringBuilder();

        await foreach (var payload in stream.ReadResultsAsync())
        {
            results.AppendLine(payload.ToJson());
            results.AppendLine();
        }

        results.ToString().MatchSnapshot();
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

        IResponseStream stream = Assert.IsType<ResponseStream>(result);
        var list = new List<(string, string)>();

        await foreach (var payload in stream.ReadResultsAsync())
        {
            var path = (payload.Path?.ToString() ?? string.Empty).Replace("/", "-");
            list.Add((path, FromResult(payload).SetHasNext(null).Create().ToJson()));
        }

        var results = new StringBuilder();

        foreach (var item in list.OrderBy(t => t.Item1))
        {
            results.AppendLine(item.Item2);
            results.AppendLine();
        }

        results.ToString().MatchSnapshot();
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

        IResponseStream stream = Assert.IsType<ResponseStream>(result);

        var results = new StringBuilder();

        await foreach (var payload in stream.ReadResultsAsync())
        {
            results.AppendLine(payload.ToJson());
            results.AppendLine();
        }

        results.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Do_Not_Defer()
    {
        Snapshot.FullName();

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

        await Assert.IsType<QueryResult>(result).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Do_Not_Defer_If_Variable_Set_To_False()
    {
        Snapshot.FullName();

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

        await Assert.IsType<QueryResult>(result).MatchSnapshotAsync();
    }

    [Fact]
    public async Task Do_Defer_If_Variable_Set_To_True()
    {
        Snapshot.FullName();

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

        await Assert.IsType<ResponseStream>(result).MatchSnapshotAsync();
    }
}
