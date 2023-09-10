using System.Threading.Tasks;
using CookieCrumble;
using Xunit;

namespace HotChocolate.Execution;

public class StreamTests
{
    /// <summary>
    /// This test shows how IAsyncEnumerable is translated to SDL
    /// </summary>
    [LocalFact]
    public async Task Schema()
    {
        var executor = await DeferAndStreamTestSchema.CreateAsync();
        executor.Schema.MatchSnapshot();
    }

    [LocalFact]
    public async Task Stream()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 300)
                }
                persons @stream {
                    id
                }
            }");

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [LocalFact]
    public async Task Stream_Nested_Defer()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 800)
                }
                personNodes(first: 1) {
                    nodes @stream {
                        ... @defer {
                            name
                        }
                    }
                }
            }");

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [LocalFact]
    public async Task Stream_InitialCount_Set_To_1()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 300)
                }
                persons @stream(initialCount: 1) {
                    id
                }
            }");

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [LocalFact]
    public async Task Stream_Label_Set_To_abc()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer {
                    wait(m: 300)
                }
                persons @stream(label: "abc") {
                    id
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [LocalFact]
    public async Task Stream_If_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                persons @stream(if: false) {
                    id
                }
            }
            """);

        Assert.IsType<QueryResult>(result).MatchSnapshot();
    }

    [LocalFact]
    public async Task Stream_If_Variable_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(
                    """
                    query ($stream: Boolean!) {
                        persons @stream(if: $stream) {
                            id
                        }
                    }
                    """)
                .SetVariableValue("stream", false)
                .Create());

        Assert.IsType<QueryResult>(result).MatchSnapshot();
    }

    [LocalFact]
    public async Task AsyncEnumerable_Result()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(
                    """
                    {
                        persons {
                            id
                        }
                    }
                    """)
                .Create());

        Assert.IsType<QueryResult>(result).MatchSnapshot();
    }
}
