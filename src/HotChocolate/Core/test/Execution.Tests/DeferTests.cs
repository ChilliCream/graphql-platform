using CookieCrumble;

namespace HotChocolate.Execution;

public class DeferTests
{
    [Fact]
    public async Task InlineFragment_Defer()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    person(id: ""UGVyc29uOjE="") {
                        id
                    }
                }
            }");

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_Nested()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    person(id: ""UGVyc29uOjE="") {
                        id
                        ... @defer {
                            name
                        }
                    }
                }
            }");

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_Label_Set_To_abc()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer(label: ""abc"") {
                    person(id: ""UGVyc29uOjE="") {
                        id
                    }
                }
            }");

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_If_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer(if: false) {
                    person(id: ""UGVyc29uOjE="") {
                        id
                    }
                }
            }");

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_If_Variable_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .Create()
                .SetDocument(
                    @"query($defer: Boolean!) {
                        ... @defer(if: $defer) {
                            person(id: ""UGVyc29uOjE="") {
                                id
                            }
                        }
                    }")
                .SetVariableValues(new Dictionary<string, object> { {"defer", false }, })
                .Build());

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... Foo @defer
            }

            fragment Foo on Query {
                person(id: ""UGVyc29uOjE="") {
                    id
                }
            }");

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_Nested()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... Foo @defer
            }

            fragment Foo on Query {
                person(id: "UGVyc29uOjE=") {
                    id
                    ... @defer {
                        name
                    }
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_Label_Set_To_abc()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... Foo @defer(label: ""abc"")
            }

            fragment Foo on Query {
                person(id: ""UGVyc29uOjE="") {
                    id
                }
            }");

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_If_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... Foo @defer(if: false)
            }

            fragment Foo on Query {
                person(id: ""UGVyc29uOjE="") {
                    id
                }
            }");

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_If_Variable_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .Create()
                .SetDocument(
                    @"query ($defer: Boolean!) {
                        ... Foo @defer(if: $defer)
                    }

                    fragment Foo on Query {
                        person(id: ""UGVyc29uOjE="") {
                            id
                        }
                    }")
                .SetVariableValues(new Dictionary<string, object> { {"defer", false }, })
                .Build());

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }
}
