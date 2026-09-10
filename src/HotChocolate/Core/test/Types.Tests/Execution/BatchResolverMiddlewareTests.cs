using System.Collections.Concurrent;
using System.Collections.Immutable;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class BatchResolverMiddlewareTests
{
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
        // two aliased batch fields share one non-null parent. One alias faults in its partitioner,
        // and because the parent chain is non-null the eager null propagation invalidates the
        // shared parent slot, so the other alias contexts are swept and the batch resolver is
        // never invoked.
        var invocationCount = 0;

        var resultTask =
            new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("user")
                        .Type<NonNullType<ObjectType<BatchUser>>>()
                        .Resolve(new BatchUser(1, "Alice"));
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
                        if (ctx.ResponseName == "first")
                        {
                            throw new GraphQLException("bad partition key");
                        }

                        return 0UL;
                    };
                })
                .ExecuteRequestAsync(
                    """
                    {
                        user {
                            first: greeting
                            second: greeting
                            third: greeting
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
                    "user",
                    "first"
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
