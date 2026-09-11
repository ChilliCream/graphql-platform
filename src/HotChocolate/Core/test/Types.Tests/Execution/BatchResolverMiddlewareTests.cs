using System.Collections.Concurrent;
using System.Collections.Immutable;
using CookieCrumble;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class BatchResolverMiddlewareTests
{
    [Fact]
    public async Task BatchResolver_Should_ExcludeCompiledArgumentErrors_When_ScalarRejectsLiteral()
    {
        // arrange
        var calls = 0;
        var executor = await new ServiceCollection().AddGraphQL()
            .AddType<RejectingStringType>()
            .AddQueryType(d =>
            {
                d.Field("users").Type<ListType<ObjectType<BatchUser>>>()
                    .Resolve(new[] { new BatchUser(1, "Alice"), new(2, "Bob") });
            })
            .AddObjectType<BatchUser>(d => d.Field("greeting").Type<StringType>()
                .Argument("text", a => a.Type<RejectingStringType>())
                .ResolveBatch(contexts =>
                {
                    calls++;
                    return new ValueTask<IReadOnlyList<ResolverResult>>(contexts.Select(c =>
                        ResolverResult.Ok(c.ArgumentValue<string>("text"))).ToArray());
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ users { name greeting(text: \"bad\") } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(calls, "Delegate calls").MatchMarkdownSnapshot();
    }

    public sealed class RejectingStringType() : StringType("RejectingString")
    {
        protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
        {
            if (valueLiteral.Value == "bad")
            {
                throw new GraphQLException("Rejected literal");
            }

            return valueLiteral.Value;
        }
    }

    [Fact]
    public async Task BatchPipeline_Should_SkipResolvedContexts_When_NoMiddlewareConfigured()
    {
        // arrange
        var calls = new List<int[]>();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("users").Type<ListType<ObjectType<BatchUser>>>()
                    .Resolve(new[] { new BatchUser(1, "Alice"), new(2, "Bob"), new(3, "Charlie") });
                d.Field("bare").Type<StringType>().Extend().Configuration.BatchResolver = contexts =>
                {
                    calls.Add(contexts.Select(c => c.Parent<BatchUser>().Id).ToArray());
                    foreach (var context in contexts)
                    {
                        context.Result = context.Parent<BatchUser>().Name;
                    }

                    return ValueTask.CompletedTask;
                };
            })
            .AddObjectType<BatchUser>(d => d.Field("name").Type<StringType>()
                .UseBatch(_ => async contexts =>
                {
                    // Invoke the middleware-free pipeline with real executing contexts.
                    var pipeline = contexts[0].Schema.QueryType.Fields["bare"].BatchResolver!;
                    contexts[1].Result = "cached";
                    contexts[2].ReportError("blocked");
                    await pipeline([]);
                    await pipeline(contexts);
                    await pipeline(contexts);
                })
                .ResolveBatch(_ => throw new GraphQLException("Unexpected terminal invocation")))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ users { name } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(calls, "Bare delegate parents").MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BatchResolver_Should_PreserveSiblingChildren_When_PureChildFails(bool throws)
    {
        // arrange
        var children = new ConcurrentQueue<string>();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Field("users").Type<ListType<ObjectType<BatchUser>>>()
                .Resolve(new[] { new BatchUser(1, "Alice"), new(2, "Bob"), new(3, "Charlie") }))
            .AddObjectType<BatchUser>(d => d.Field("profile")
                .Type<NonNullType<ObjectType<BatchProfile>>>()
                .ResolveBatch(contexts => new ValueTask<IReadOnlyList<ResolverResult>>(contexts.Select(c =>
                {
                    var user = c.Parent<BatchUser>();
                    return ResolverResult.Ok(new BatchProfile(user.Name, user.Id));
                }).ToArray())))
            .AddObjectType<BatchProfile>(d =>
            {
                d.Field(p => p.DisplayName).Resolve(async context =>
                {
                    await Task.Yield();
                    var name = context.Parent<BatchProfile>().DisplayName;
                    children.Enqueue(name);
                    return name;
                });
                d.Field("check").Type<NonNullType<StringType>>()
                    .Resolve(_ => "regular")
                    .Extend().Configuration.PureResolver = context =>
                    {
                        if (context.Parent<BatchProfile>().Score == 2)
                        {
                            if (throws)
                            {
                                throw new GraphQLException("Child failed");
                            }

                            return null;
                        }

                        return "ok";
                    };
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ users { profile { displayName check } } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.True(((ObjectType)executor.Schema.Types["BatchProfile"]).Fields["check"].PureResolver is not null);
        new Snapshot(postFix: throws.ToString()).Add(result, "Result")
            .Add(children.Order().ToArray(), "Executed children").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ResolveBatch_Should_IgnoreRegularPipeline_When_RegularResolverAlsoConfigured()
    {
        // arrange
        var compiled = 0;
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Field("value").Type<StringType>()
                .Resolve("regular")
                .Use(next =>
                {
                    compiled++;
                    return next;
                })
                .ResolveBatch(contexts => new ValueTask<IReadOnlyList<ResolverResult>>(
                    contexts.Select(_ => ResolverResult.Ok("batch")).ToArray())))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ value }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(compiled, "Regular middleware compilations")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("none", false)]
    [InlineData("value", false)]
    [InlineData("null", false)]
    [InlineData("error", false)]
    [InlineData("errorValue", false)]
    [InlineData("all", false)]
    [InlineData("allNull", false)]
    [InlineData("allError", false)]
    [InlineData("after", false)]
    [InlineData("null", true)]
    [InlineData("error", true)]
    [InlineData("errorValue", true)]
    [InlineData("recoverNull", true)]
    [InlineData("recoverError", true)]
    public async Task BatchResolver_Should_CompleteEachContext_When_MiddlewareShortCircuits(
        string mode,
        bool nonNull)
    {
        // arrange
        var invocations = new List<int[]>();
        var children = new ConcurrentQueue<string>();
        var after = new List<string?>();
        var sameArray = false;
        ImmutableArray<IMiddlewareContext> original = default;
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Field("users")
                .Type<ListType<ObjectType<BatchUser>>>()
                .Resolve(new[] { new BatchUser(1, "Alice"), new(2, "Bob"), new(3, "Charlie") }))
            .AddObjectType<BatchUser>(d =>
            {
                var field = d.Field("profile");
                if (nonNull)
                {
                    field.Type<NonNullType<ObjectType<BatchProfile>>>();
                }
                else
                {
                    field.Type<ObjectType<BatchProfile>>();
                }

                field.UseBatch(next => async contexts =>
                {
                    original = contexts;
                    foreach (var context in contexts)
                    {
                        if (mode.StartsWith("all", StringComparison.Ordinal) || context.Parent<BatchUser>().Id == 2)
                        {
                            switch (mode)
                            {
                                case "value":
                                case "all":
                                    context.Result = new BatchProfile("cached", 0);
                                    break;
                                case "null":
                                case "allNull":
                                case "recoverNull":
                                    context.Result = null;
                                    break;
                                case "error":
                                case "allError":
                                case "recoverError":
                                    context.ReportError("blocked");
                                    break;
                                case "errorValue":
                                    context.Result = ErrorBuilder.New().SetMessage("blocked").Build();
                                    break;
                            }
                        }
                    }

                    await next(contexts);
                    foreach (var context in contexts)
                    {
                        after.Add(context.Result switch
                        {
                            BatchProfile profile => profile.DisplayName,
                            IError error => error.Message,
                            _ => null
                        });
                    }

                    if (mode is "after" or "recoverNull" or "recoverError")
                    {
                        contexts[1].Result = new BatchProfile("after", 0);
                    }
                });
                field.Extend().Configuration.BatchResolver = contexts =>
                {
                    sameArray = contexts == original;
                    invocations.Add(contexts.Select(c => c.Parent<BatchUser>().Id).ToArray());
                    foreach (var context in contexts)
                    {
                        var user = context.Parent<BatchUser>();
                        context.Result = new BatchProfile(user.Name, user.Id);
                    }

                    return ValueTask.CompletedTask;
                };
            })
            .AddObjectType<BatchProfile>(d => d.Field(p => p.DisplayName).Resolve(async context =>
            {
                await Task.Yield();
                var name = context.Parent<BatchProfile>().DisplayName;
                children.Enqueue(name);
                return name;
            }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ users { profile { displayName } } }",
            cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        new Snapshot(postFix: $"{mode}_{nonNull}")
            .Add(result, "Result")
            .Add(invocations, "Delegate parents")
            .Add(after, "Full array after next")
            .Add(sameArray, "Delegate received original array")
            .Add(children.Order().ToArray(), "Non-pure children")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(-1, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    public async Task BatchResolver_Should_ExcludeInvalidArguments_When_VariableSetsShareSelection(
        int invalidId,
        bool nonNull)
    {
        // arrange
        var invocations = new List<int[]>();
        var middleware = new List<int[]>();
        var partitions = new List<int>();
        var children = new ConcurrentQueue<string>();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d =>
            {
                var field = d.Field("profile")
                    .Argument("id", a => a.Type<NonNullType<IntType>>())
                    .UseResolverScope()
                    .UseBatch(next => contexts =>
                    {
                        middleware.Add(contexts.Select(c => c.ArgumentValue<int>("id")).ToArray());
                        return next(contexts);
                    })
                    .ResolveBatch(contexts =>
                    {
                        invocations.Add(contexts.Select(c => c.ArgumentValue<int>("id")).ToArray());
                        return new ValueTask<IReadOnlyList<ResolverResult>>(contexts.Select(c =>
                            ResolverResult.Ok(new BatchProfile($"Profile {c.ArgumentValue<int>("id")}", 0))).ToArray());
                    });
                if (nonNull)
                {
                    field.Type<NonNullType<ObjectType<BatchProfile>>>();
                }
                else
                {
                    field.Type<ObjectType<BatchProfile>>();
                }

                field.Extend().Configuration.BatchPartitionKeyResolver = context =>
                {
                    var id = context.ArgumentValue<int>("id");
                    partitions.Add(id);
                    if (invalidId == 4 && id == 3)
                    {
                        throw new GraphQLException("Partition 3 failed");
                    }

                    return 0;
                };
            })
            .AddObjectType<BatchProfile>(d => d.Field(p => p.DisplayName).Resolve(async context =>
            {
                await Task.Yield();
                var name = context.Parent<BatchProfile>().DisplayName;
                children.Enqueue(name);
                return name;
            }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument("query($id:Int=1){ profile(id:$id){displayName} }")
            .SetVariableValues(Enumerable.Range(1, 3).Select(id =>
                (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["id"] = invalidId == -1 || invalidId == id
                        || (invalidId == 4 && id == 1) || (invalidId == 5 && id < 3) ? null : id
                }).ToList())
            .Build(), cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        new Snapshot(postFix: $"{invalidId}_{nonNull}")
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .Add(batch.Results[2], "Set 2")
            .Add(middleware, "Middleware arguments")
            .Add(partitions, "Partition arguments")
            .Add(invocations, "Delegate arguments")
            .Add(children.Order().ToArray(), "Non-pure children")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task BatchResolver_Should_Not_Dispatch_When_Every_Partition_Key_Throws()
    {
        // arrange
        // the partitioner throws for every sibling context, so none survives to be dispatched.
        var invocationCount = 0;

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<BatchUser>>>()
                        .Resolve(new List<BatchUser>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<BatchUser>(d =>
                {
                    d.Field(u => u.Name);

                    var field = d.Field("greeting").Type<StringType>();

                    field.Extend().Configuration.BatchResolver = contexts =>
                    {
                        Interlocked.Increment(ref invocationCount);

                        foreach (var ctx in contexts)
                        {
                            ctx.Result = $"Hello, {ctx.Parent<BatchUser>().Name}!";
                        }

                        return ValueTask.CompletedTask;
                    };

                    field.Extend().Configuration.BatchPartitionKeyResolver =
                        _ => throw new GraphQLException("bad partition key");
                })
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // a no-survivor batch must complete and not hang the work loop, so it is guarded.
        var resultTask = executor.ExecuteAsync(
            """
            {
                users {
                    name
                    greeting
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);
        var result = await resultTask.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, invocationCount);
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "bad partition key",
                  "path": [
                    "users",
                    0,
                    "greeting"
                  ]
                },
                {
                  "message": "bad partition key",
                  "path": [
                    "users",
                    1,
                    "greeting"
                  ]
                },
                {
                  "message": "bad partition key",
                  "path": [
                    "users",
                    2,
                    "greeting"
                  ]
                }
              ],
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": null
                  },
                  {
                    "name": "Bob",
                    "greeting": null
                  },
                  {
                    "name": "Charlie",
                    "greeting": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Keep_Survivor_Subtrees_When_NonNullableContextFaults()
    {
        // arrange
        // a non-nullable batch field returns an object with its own child selections. The
        // partitioner faults for exactly one of three siblings, so only that context propagates
        // null while the two survivors must reach the batch resolver with full child data and the
        // batch must observe exactly the two survivors.
        var receivedCounts = new List<int>();

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<BatchUser>>>()
                        .Resolve(new List<BatchUser>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<BatchUser>(d =>
                {
                    d.Field(u => u.Name);

                    var field = d.Field("profile").Type<NonNullType<ObjectType<BatchProfile>>>();

                    field.Extend().Configuration.BatchResolver = contexts =>
                    {
                        lock (receivedCounts)
                        {
                            receivedCounts.Add(contexts.Length);
                        }

                        foreach (var ctx in contexts)
                        {
                            var user = ctx.Parent<BatchUser>();
                            ctx.Result = new BatchProfile(user.Name, user.Id * 100);
                        }

                        return ValueTask.CompletedTask;
                    };

                    field.Extend().Configuration.BatchPartitionKeyResolver = ctx =>
                    {
                        if (ctx.Parent<BatchUser>().Id == 2)
                        {
                            throw new GraphQLException("bad partition key");
                        }

                        return 0UL;
                    };
                })
                .AddObjectType<BatchProfile>(d =>
                {
                    d.Field(p => p.DisplayName);
                    d.Field(p => p.Score);
                })
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                users {
                    name
                    profile {
                        displayName
                        score
                    }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, Assert.Single(receivedCounts));
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "bad partition key",
                  "path": [
                    "users",
                    1,
                    "profile"
                  ]
                }
              ],
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "profile": {
                      "displayName": "Alice",
                      "score": 100
                    }
                  },
                  null,
                  {
                    "name": "Charlie",
                    "profile": {
                      "displayName": "Charlie",
                      "score": 300
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Skip_Dispatch_When_SharedParentInvalidatedBySibling()
    {
        // arrange
        // One partition fault invalidates the shared non-null list before dispatch.
        var invocationCount = 0;

        var resultTask =
            new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<NonNullType<ListType<NonNullType<ObjectType<BatchUser>>>>>()
                        .Resolve(new[] { new BatchUser(1, "Alice"), new(2, "Bob"), new(3, "Charlie") });
                })
                .AddObjectType<BatchUser>(d =>
                {
                    d.Field(u => u.Name);

                    var field = d.Field("greeting").Type<NonNullType<StringType>>();

                    field.Extend().Configuration.BatchResolver = contexts =>
                    {
                        Interlocked.Increment(ref invocationCount);

                        foreach (var ctx in contexts)
                        {
                            ctx.Result = $"Hello, {ctx.Parent<BatchUser>().Name}!";
                        }

                        return ValueTask.CompletedTask;
                    };

                    field.Extend().Configuration.BatchPartitionKeyResolver = ctx =>
                    {
                        if (ctx.Parent<BatchUser>().Id == 2)
                        {
                            throw new GraphQLException("bad partition key");
                        }

                        return 0UL;
                    };
                })
                .ExecuteRequestAsync(
                    """
                    {
                        users {
                            greeting
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await resultTask.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, invocationCount);
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "bad partition key",
                  "path": [
                    "users",
                    1,
                    "greeting"
                  ]
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Isolate_Faulted_Context_When_Partitioner_Throws()
    {
        // arrange
        // the partitioner throws for exactly one of three sibling contexts.
        var receivedCounts = new List<int>();

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<BatchUser>>>()
                        .Resolve(new List<BatchUser>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<BatchUser>(d =>
                {
                    d.Field(u => u.Name);

                    var field = d.Field("greeting").Type<StringType>();

                    field.Extend().Configuration.BatchResolver = contexts =>
                    {
                        lock (receivedCounts)
                        {
                            receivedCounts.Add(contexts.Length);
                        }

                        foreach (var ctx in contexts)
                        {
                            ctx.Result = $"Hello, {ctx.Parent<BatchUser>().Name}!";
                        }

                        return ValueTask.CompletedTask;
                    };

                    field.Extend().Configuration.BatchPartitionKeyResolver = ctx =>
                    {
                        if (ctx.Parent<BatchUser>().Id == 2)
                        {
                            throw new GraphQLException("bad partition key");
                        }

                        return 0UL;
                    };
                })
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var resultTask = executor.ExecuteAsync(
            """
            {
                users {
                    name
                    greeting
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);
        var result = await resultTask.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, Assert.Single(receivedCounts));
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "bad partition key",
                  "path": [
                    "users",
                    1,
                    "greeting"
                  ]
                }
              ],
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": null
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ResolveBatch_Should_ApplyErrorFilter_When_ResultIsFail()
    {
        // act
        // an IErrorFilter must rewrite errors produced via ResolverResult.Fail the same
        // way it rewrites regular resolver errors, leaving sibling results intact.
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddErrorFilter(e => e.WithCode("FILTERED:" + e.Code))
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<BatchUser>>>()
                        .Resolve(new List<BatchUser>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<BatchUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .Type<StringType>()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var user = contexts[i].Parent<BatchUser>();
                                results[i] = user.Id == 2
                                    ? ResolverResult.Fail(
                                        ErrorBuilder.New()
                                            .SetMessage("no greeting")
                                            .SetCode("USER_BLOCKED")
                                            .Build())
                                    : ResolverResult.Ok($"Hello, {user.Name}!");
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .ExecuteRequestAsync(
                    """
                    {
                        users {
                            name
                            greeting
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "no greeting",
                  "path": [
                    "users",
                    1,
                    "greeting"
                  ],
                  "extensions": {
                    "code": "FILTERED:USER_BLOCKED"
                  }
                }
              ],
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": null
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Share_SingleResolverScope_When_UseResolverScopeApplied()
    {
        // arrange
        // with resolver-scoped DI a batch is one resolver invocation, so all contexts share
        // a single service scope distinct from the request scope. Capturing the probe from the
        // request scope makes the test fail if UseResolverScope is silently ignored, because
        // then the batch would observe the request-scoped instance instead of a fresh one.
        var observed = new ConcurrentBag<int>();
        var requestScopedInstanceId = 0;

        var result =
            await new ServiceCollection()
                .AddScoped<ScopeProbe>()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<BatchUser>>>()
                        .Resolve(ctx =>
                        {
                            requestScopedInstanceId =
                                ctx.Services.GetRequiredService<ScopeProbe>().InstanceId;
                            return new List<BatchUser>
                            {
                                new(1, "Alice"),
                                new(2, "Bob"),
                                new(3, "Charlie")
                            };
                        });
                })
                .AddObjectType<BatchUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .Type<StringType>()
                        .UseResolverScope()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var probe = contexts[i].Services.GetRequiredService<ScopeProbe>();
                                observed.Add(probe.InstanceId);
                                results[i] = ResolverResult.Ok($"Hello, {contexts[i].Parent<BatchUser>().Name}!");
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .ExecuteRequestAsync(
                    """
                    {
                        users {
                            greeting
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var batchInstanceId = Assert.Single(observed.Distinct());
        Assert.NotEqual(requestScopedInstanceId, batchInstanceId);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "greeting": "Hello, Bob!"
                  },
                  {
                    "greeting": "Hello, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task UseBatch_Should_ComposeMiddlewareInOrder_When_MultipleRegistered()
    {
        // act
        // first registered batch middleware is outermost, so values are wrapped A(B(value))
        // and each context is decorated individually.
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<BatchUser>>>()
                        .Resolve(new List<BatchUser>
                        {
                            new(1, "Alice"),
                            new(2, "Bob")
                        });
                })
                .AddObjectType<BatchUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .Type<StringType>()
                        .UseBatch(next => async contexts =>
                        {
                            await next(contexts);

                            foreach (var ctx in contexts)
                            {
                                ctx.Result = $"A({ctx.Result})";
                            }
                        })
                        .UseBatch(next => async contexts =>
                        {
                            await next(contexts);

                            foreach (var ctx in contexts)
                            {
                                ctx.Result = $"B({ctx.Result})";
                            }
                        })
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                results[i] = ResolverResult.Ok(contexts[i].Parent<BatchUser>().Name);
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .ExecuteRequestAsync(
                    """
                    {
                        users {
                            greeting
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "greeting": "A(B(Alice))"
                  },
                  {
                    "greeting": "A(B(Bob))"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task UseBatch_Should_DedupeByKey_When_TwoNonRepeatableConfigsShareKey()
    {
        // arrange
        // two non-repeatable batch middleware configs with the same key collapse to one,
        // so the wrapping marker is applied exactly once.
        var executionCount = 0;

        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<BatchUser>>>()
                        .Resolve(new List<BatchUser>
                        {
                            new(1, "Alice"),
                            new(2, "Bob")
                        });
                })
                .AddObjectType<BatchUser>(d =>
                {
                    d.Field(u => u.Name);

                    var fieldDescriptor =
                        d.Field("greeting")
                            .Type<StringType>();

                    BatchFieldMiddleware marker = next => async contexts =>
                    {
                        executionCount++;
                        await next(contexts);

                        foreach (var ctx in contexts)
                        {
                            ctx.Result = $"M({ctx.Result})";
                        }
                    };

                    fieldDescriptor.Extend().Configuration.BatchMiddlewareConfigurations.Add(
                        new BatchFieldMiddlewareConfiguration(marker, isRepeatable: false, key: "marker"));
                    fieldDescriptor.Extend().Configuration.BatchMiddlewareConfigurations.Add(
                        new BatchFieldMiddlewareConfiguration(marker, isRepeatable: false, key: "marker"));

                    fieldDescriptor.ResolveBatch(contexts =>
                    {
                        var results = new ResolverResult[contexts.Count];

                        for (var i = 0; i < contexts.Count; i++)
                        {
                            results[i] = ResolverResult.Ok(contexts[i].Parent<BatchUser>().Name);
                        }

                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
                })
                .ExecuteRequestAsync(
                    """
                    {
                        users {
                            greeting
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, executionCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "greeting": "M(Alice)"
                  },
                  {
                    "greeting": "M(Bob)"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_PostProcess_ListResults_When_ElementTypeIsEnumerable()
    {
        // act
        // an inferred list post-processor must materialize each batched context's lazy
        // enumerable into its own array, mirroring regular ResolverTask behavior.
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<BatchUser>>>()
                        .Resolve(new List<BatchUser>
                        {
                            new(1, "Alice"),
                            new(2, "Bob")
                        });
                })
                .AddObjectType<BatchUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("tags")
                        .Type<ListType<StringType>>()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var user = contexts[i].Parent<BatchUser>();
                                results[i] = ResolverResult.Ok(LazyTags(user.Name));
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .ExecuteRequestAsync(
                    """
                    {
                        users {
                            tags
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "tags": [
                      "tag:Alice:0",
                      "tag:Alice:1"
                    ]
                  },
                  {
                    "tags": [
                      "tag:Bob:0",
                      "tag:Bob:1"
                    ]
                  }
                ]
              }
            }
            """);
    }

    private static IEnumerable<string> LazyTags(string name)
    {
        for (var i = 0; i < 2; i++)
        {
            yield return $"tag:{name}:{i}";
        }
    }

    public record BatchUser(int Id, string Name);

    public record BatchProfile(string DisplayName, int Score);

    public sealed class ScopeProbe
    {
        private static int s_counter;

        public int InstanceId { get; } = Interlocked.Increment(ref s_counter);
    }
}
