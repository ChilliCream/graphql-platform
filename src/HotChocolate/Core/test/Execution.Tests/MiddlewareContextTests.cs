using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution;

public class MiddlewareContextTests
{
    [Fact]
    public async Task AccessVariables()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                "type Query { foo(bar: String) : String }")
            .AddResolver(
                "Query",
                "foo",
                ctx =>
                    ctx.Variables.GetVariable<string>("abc"))
            .Create();

        var request = OperationRequestBuilder.New()
            .SetDocument("query abc($abc: String){ foo(bar: $abc) }")
            .SetVariableValues(new Dictionary<string, object?> { {"abc", "def" }, })
            .Build();

        // act
        var result = await schema.MakeExecutable().ExecuteAsync(request);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task AccessVariables_Fails_When_Variable_Not_Exists()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                "type Query { foo(bar: String) : String }")
            .AddResolver(
                "Query",
                "foo",
                ctx =>
                    ctx.Variables.GetVariable<string>("abc"))
            .Create();

        var request = OperationRequestBuilder.New()
            .SetDocument("query abc($def: String){ foo(bar: $def) }")
            .SetVariableValues(new Dictionary<string, object?> { {"def", "ghi" }, })
            .Build();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(request);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task CollectFields()
    {
        // arrange
        var list = new List<ISelection>();

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                """
                type Query {
                    foo: Foo
                }

                type Foo {
                    bar: Bar
                }

                type Bar {
                    baz: String
                }
                """)
            .Use(
                _ => context =>
                {
                    if (context.Selection.Type.NamedType() is ObjectType type)
                    {
                        foreach (var selection in context.GetSelections(type))
                        {
                            CollectSelections(context, selection, list);
                        }
                    }
                    return default;
                })
            .Create();

        // act
        await schema.MakeExecutable().ExecuteAsync(
            @"{
                foo {
                    bar {
                        baz
                    }
                }
            }");

        // assert
        list.Select(t => t.SyntaxNode.Name.Value).ToList().MatchSnapshot();
    }

    [Fact]
    public async Task CustomServiceProvider()
    {
        // arrange
        var services = new DictionaryServiceProvider(typeof(string), "hello");

        // assert
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    d =>
                    {
                        d.Name(OperationTypeNames.Query);

                        d.Field("foo")
                            .Resolve(ctx => ctx.Service<string>())
                            .Use(
                                next => async context =>
                                {
                                    context.Services = services;
                                    await next(context);
                                });
                    })
                .ExecuteRequestAsync("{ foo }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ReplaceArguments_Delegate()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                var original =
                                    context.ReplaceArguments(
                                        current =>
                                        {
                                            var arguments = new Dictionary<string, ArgumentValue>();

                                            foreach (var argumentValue in current.Values)
                                            {
                                                if (argumentValue.Type.RuntimeType ==
                                                    typeof(string) &&
                                                    argumentValue
                                                        .ValueLiteral is StringValueNode sv)
                                                {
                                                    sv = sv.WithValue(sv.Value.Trim());
                                                    var trimmedArgument = new ArgumentValue(
                                                        argumentValue,
                                                        ValueKind.String,
                                                        false,
                                                        false,
                                                        null,
                                                        sv);
                                                    arguments.Add(
                                                        argumentValue.Name,
                                                        trimmedArgument);
                                                }
                                                else
                                                {
                                                    arguments.Add(
                                                        argumentValue.Name,
                                                        argumentValue);
                                                }
                                            }

                                            return arguments;
                                        });

                                await next(context);

                                context.ReplaceArguments(original);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc   \") }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task ReplaceArguments_Delegate_ReplaceWithNull_ShouldFail()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                var original = context.ReplaceArguments(_ => null!);

                                await next(context);

                                context.ReplaceArguments(original);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc   \") }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SetResultContextData()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                context.OperationResult.SetResultState("abc", "def");
                                await next(context);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc\") }");

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData.TryGetValue("abc", out var value));
        Assert.Equal("def", value);
    }

    [Fact]
    public async Task SetResultContextData_Delegate_IntValue()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                context.OperationResult.SetResultState("abc", 1);
                                context.OperationResult.SetResultState("abc",
                                    (_, c) =>
                                    {
                                        if (c is int i)
                                        {
                                            return ++i;
                                        }
                                        return 0;
                                    });
                                await next(context);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc\") }");

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData.TryGetValue("abc", out var value));
        Assert.Equal(2, value);
    }

    [Fact]
    public async Task SetResultContextData_Delegate_IntValue_When_Deferred()
    {
        using var cts = new CancellationTokenSource(5000);
        var ct = cts.Token;

        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                context.OperationResult.SetResultState("abc", 1);
                                context.OperationResult.SetResultState("abc",
                                    (_, c) =>
                                    {
                                        if (c is int i)
                                        {
                                            return ++i;
                                        }
                                        return 0;
                                    });
                                await next(context);
                            });
                })
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                })
            .ExecuteRequestAsync("{ ... @defer { abc(a: \"abc\") } }", cancellationToken: ct);

        var first = true;

        await foreach (var queryResult in result.ExpectResponseStream()
            .ReadResultsAsync().WithCancellation(cancellationToken: ct))
        {
            if (first)
            {
                first = false;
                continue;
            }

            Assert.NotNull(queryResult.Incremental?[0].ContextData);
            Assert.True(queryResult.Incremental[0].ContextData!.TryGetValue("abc", out var value));
            Assert.Equal(2, value);
        }
    }

    [Fact]
    public async Task SetResultContextData_Delegate_IntValue_WithState()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                context.OperationResult.SetResultState("abc", 1);
                                context.OperationResult.SetResultState("abc", 5,
                                    (_, c, s) =>
                                    {
                                        if (c is int i)
                                        {
                                            return i + s;
                                        }
                                        return 0;
                                    });
                                await next(context);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc\") }");

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData.TryGetValue("abc", out var value));
        Assert.Equal(6, value);
    }

    [Fact]
    public async Task SetResultExtensionData_With_IntValue()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                context.OperationResult.SetExtension("abc", 1);
                                await next(context);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc\") }");

        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                @"{
                  ""data"": {
                    ""abc"": ""abc""
                  },
                  ""extensions"": {
                    ""abc"": 1
                  }
                }");
    }

    [Fact]
    public async Task SetResultExtensionData_With_Delegate_IntValue()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                context.OperationResult.SetExtension("abc", 1);
                                context.OperationResult.SetExtension<int>("abc", (_, v) => 1 + v);
                                await next(context);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc\") }");

        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                @"{
                  ""data"": {
                    ""abc"": ""abc""
                  },
                  ""extensions"": {
                    ""abc"": 2
                  }
                }");
    }

    [Fact]
    public async Task SetResultExtensionData_With_Delegate_IntValue_With_State()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                context.OperationResult.SetExtension("abc", 1);
                                context.OperationResult.SetExtension<int, int>(
                                    key: "abc",
                                    state: 5,
                                    (_, v, s) => s + v);
                                await next(context);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc\") }");

        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                @"{
                  ""data"": {
                    ""abc"": ""abc""
                  },
                  ""extensions"": {
                    ""abc"": 6
                  }
                }");
    }

    [Fact]
    public async Task SetResultExtensionData_With_Delegate_NoDefaultValue_IntValue()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                context.OperationResult.SetExtension<int>("abc", (_, v) => 1 + v);
                                await next(context);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc\") }");

        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                @"{
                  ""data"": {
                    ""abc"": ""abc""
                  },
                  ""extensions"": {
                    ""abc"": 1
                  }
                }");
    }

    [Fact]
    public async Task SetResultExtensionData_With_ObjectValue()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                context.OperationResult.SetExtension("abc", new SomeData("def"));
                                await next(context);
                            });
                })
            .ExecuteRequestAsync("{ abc(a: \"abc\") }");

        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                @"{
                  ""data"": {
                    ""abc"": ""abc""
                  },
                  ""extensions"": {
                    ""abc"": {
                      ""someField"": ""def""
                    }
                  }
                }");
    }

    [Fact]
    public async Task SetResultExtensionData_With_ObjectValue_WhenDeferred()
    {
        using var cts = new CancellationTokenSource(5000);
        var ct = cts.Token;

        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d =>
                {
                    d.Field("abc")
                        .Argument("a", t => t.Type<StringType>())
                        .Resolve(ctx => ctx.ArgumentValue<string>("a"))
                        .Use(
                            next => async context =>
                            {
                                context.OperationResult.SetExtension("abc", new SomeData("def"));
                                await next(context);
                            });
                })
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                })
            .ExecuteRequestAsync("{ ... @defer { abc(a: \"abc\") } }", cancellationToken: ct);

        var first = true;
        await foreach (var queryResult in result.ExpectResponseStream()
            .ReadResultsAsync().WithCancellation(ct))
        {
            if (first)
            {
                first = false;
                continue;
            }

            Snapshot
                .Create()
                .AddResult(queryResult)
                .MatchInline(
                    @"{
                      ""incremental"": [
                        {
                          ""data"": {
                            ""abc"": ""abc""
                          },
                          ""extensions"": {
                            ""abc"": {
                              ""someField"": ""def""
                            }
                          },
                          ""path"": []
                        }
                      ],
                      ""hasNext"": false
                    }");
        }
    }

    private static void CollectSelections(
        IResolverContext context,
        ISelection selection,
        ICollection<ISelection> collected)
    {
        if (selection.Type.IsLeafType())
        {
            collected.Add(selection);
        }

        if (selection.Type.NamedType() is ObjectType objectType)
        {
            foreach (var child in context.GetSelections(objectType, selection))
            {
                CollectSelections(context, child, collected);
            }
        }
    }

    private record SomeData(string SomeField);
}
