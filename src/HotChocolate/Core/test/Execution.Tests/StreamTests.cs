namespace HotChocolate.Execution;

public class StreamTests
{
    /// <summary>
    /// This test shows how IAsyncEnumerable is translated to SDL
    /// </summary>
    [Fact]
    public async Task Schema()
    {
        var executor = await DeferAndStreamTestSchema.CreateAsync();
        executor.Schema.MatchSnapshot();
    }

    [Fact]
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
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchAggregatedSnapshot();
    }

    [Fact]
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
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchAggregatedSnapshot();
    }

    [Fact]
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
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchAggregatedSnapshot();
    }

    [Fact]
    public async Task Stream_InitialCount_Exceeds_Total_Count()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 300)
                }
                persons @stream(initialCount: 7) {
                    id
                }
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchAggregatedSnapshot();
    }

    [Fact]
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
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchAggregatedSnapshot();
    }

    [Fact]
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
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_If_Variable_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($stream: Boolean!) {
                        persons @stream(if: $stream) {
                            id
                        }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { { "stream", false } })
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }

    [Fact]
    public async Task AsyncEnumerable_Result()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        persons {
                            id
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }
}
