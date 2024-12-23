using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
            """
            {
                ... @defer {
                    person(id: "UGVyc29uOjE=") {
                        id
                    }
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_Nested()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer {
                    person(id: "UGVyc29uOjE=") {
                        id
                        ... @defer {
                            name
                        }
                    }
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
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

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_If_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer(if: false) {
                    person(id: "UGVyc29uOjE=") {
                        id
                    }
                }
            }
            """);

        Assert.IsType<OperationResult>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_If_Variable_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query($defer: Boolean!) {
                        ... @defer(if: $defer) {
                            person(id: "UGVyc29uOjE=") {
                                id
                            }
                        }
                    }
                    """)
                .SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        { "defer", false },
                    })
                .Build());

        Assert.IsType<OperationResult>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer()
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
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
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

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_Label_Set_To_abc()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... Foo @defer(label: "abc")
            }

            fragment Foo on Query {
                person(id: "UGVyc29uOjE=") {
                    id
                }
            }
            """);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_If_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... Foo @defer(if: false)
            }

            fragment Foo on Query {
                person(id: "UGVyc29uOjE=") {
                    id
                }
            }
            """);

        Assert.IsType<OperationResult>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_If_Variable_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($defer: Boolean!) {
                        ... Foo @defer(if: $defer)
                    }

                    fragment Foo on Query {
                        person(id: "UGVyc29uOjE=") {
                            id
                        }
                    }
                    """)
                .SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        { "defer", false },
                    })
                .Build());

        Assert.IsType<OperationResult>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Ensure_GlobalState_Is_Passed_To_DeferContext_Stacked_Defer()
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
                        ... @defer {
                            ensureState {
                                ... @defer {
                                    state
                                }
                            }
                        }
                    }
                    """)
                .SetGlobalState("requestState", "state 123")
                .Build());

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Ensure_GlobalState_Is_Passed_To_DeferContext_Stacked_Defer_2()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        await using var response = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        ... @defer {
                            e: ensureState {
                                ... @defer {
                                    more {
                                        ... @defer {
                                            stuff
                                        }
                                    }
                                }
                            }
                        }
                    }
                    """)
                .SetGlobalState("requestState", "state 123")
                .Build());

        Assert.IsType<ResponseStream>(response).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Ensure_GlobalState_Is_Passed_To_DeferContext_Single_Defer()
    {
        // this test ensures that the request context is not recycled until the
        // a stream is fully processed when no outer DI scope exists.

        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        ensureState {
                            ... @defer {
                                state
                            }
                        }
                    }
                    """)
                .SetGlobalState("requestState", "state 123")
                .Build());

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Ensure_GlobalState_Is_Passed_To_DeferContext_Single_Defer_2()
    {
        // this test ensures that the request context is not recycled until the
        // a stream is fully processed when an outer DI scope exists.

        // arrange
        var services = DeferAndStreamTestSchema.CreateServiceProvider();
        var executor = await services.GetRequestExecutorAsync();
        await using var scope = services.CreateAsyncScope();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        ... @defer {
                            ensureState {
                                state
                            }
                        }
                    }
                    """)
                .SetGlobalState("requestState", "state 123")
                .SetServices(scope.ServiceProvider)
                .Build());

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    private class StateRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            requestBuilder.AddGlobalState("requestState", "bar");
            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }
}
