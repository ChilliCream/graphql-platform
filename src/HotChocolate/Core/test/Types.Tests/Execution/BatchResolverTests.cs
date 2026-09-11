using System.Text.Json.Nodes;
using GreenDonut;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class BatchResolverTests
{
    [Fact]
    public async Task BatchResolver_Should_Resolve_Nested_Field()
    {
        // arrange & act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<User>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<User>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .Type<StringType>()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var user = contexts[i].Parent<User>();
                                results[i] = ResolverResult.Ok($"Hello, {user.Name}!");
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
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task BatchResolver_Should_Separate_Batches_When_Field_Arguments_Are_Aliased()
    {
        // arrange
        ProductByIdQuery.BatchCallCount = 0;

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ProductByIdQuery>()
                .ExecuteRequestAsync(
                    """
                    {
                        a: productById(id: 1) {
                            name
                        }
                        b: productById(id: 2) {
                            name
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, ProductByIdQuery.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": {
                  "name": "Product 1"
                },
                "b": {
                  "name": "Product 2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Separate_Batches_When_Field_Variable_Arguments_Are_Aliased()
    {
        // arrange
        ProductByIdQuery.BatchCallCount = 0;

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ProductByIdQuery>()
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query($x: Int! $y: Int!) {
                                a: productById(id: $x) {
                                    name
                                }
                                b: productById(id: $y) {
                                    name
                                }
                            }
                            """)
                        .SetVariableValues(
                            new Dictionary<string, object?>
                            {
                                { "x", 1 },
                                { "y", 2 }
                            })
                        .Build(),
                        cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, ProductByIdQuery.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": {
                  "name": "Product 1"
                },
                "b": {
                  "name": "Product 2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Invoke_Remaining_Selection_When_One_Alias_Is_Skipped()
    {
        // arrange
        ProductByIdQuery.BatchCallCount = 0;

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ProductByIdQuery>()
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query($skip: Boolean!) {
                                a: productById(id: 1) @skip(if: $skip) {
                                    name
                                }
                                b: productById(id: 2) {
                                    name
                                }
                            }
                            """)
                        .SetVariableValues(
                            new Dictionary<string, object?>
                            {
                                { "skip", true }
                            })
                        .Build(),
                        cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, ProductByIdQuery.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "b": {
                  "name": "Product 2"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Not_Invoke_Batch_When_All_Siblings_Are_Skipped()
    {
        // arrange
        // when every sibling at the batch selection path is excluded the batch must
        // not be invoked at all and the request must complete without hanging.
        ProductByIdQuery.BatchCallCount = 0;

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ProductByIdQuery>()
                .ExecuteRequestAsync(
                    """
                    {
                        a: productById(id: 1) @skip(if: true) {
                            name
                        }
                        b: productById(id: 2) @skip(if: true) {
                            name
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, ProductByIdQuery.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {}
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Stay_Unconditional_When_Merged_From_Conditional_And_Unconditional()
    {
        // arrange
        // the same batch field is selected at the root twice with the same args: once
        // unconditionally and once inside an @include(if: $flag) fragment. The two selections
        // merge into one, which must stay unconditional per commit ed2079a5c9, so with
        // flag=false the field is still resolved and the batch runs exactly once.
        ProductByIdQuery.BatchCallCount = 0;

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ProductByIdQuery>()
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query($flag: Boolean!) {
                                productById(id: 1) {
                                    name
                                }
                                ... @include(if: $flag) {
                                    productById(id: 1) {
                                        name
                                    }
                                }
                            }
                            """)
                        .SetVariableValues(
                            new Dictionary<string, object?>
                            {
                                { "flag", false }
                            })
                        .Build(),
                        cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, ProductByIdQuery.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "productById": {
                  "name": "Product 1"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ResolveBatch_Should_Isolate_Error_To_Failing_Context()
    {
        // act
        // ResolverResult.Fail for one context must null only that field and emit a
        // single per-context error while siblings deliver their data.
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<User>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<User>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .Type<StringType>()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var user = contexts[i].Parent<User>();
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
                    "code": "USER_BLOCKED"
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
    public async Task ResolveBatch_Should_Fail_All_Contexts_When_Result_Count_Mismatches()
    {
        // act
        // returning fewer results than contexts is a developer error that fails the
        // whole dispatch: every context gets a masked error and null data.
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<User>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<User>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .Type<StringType>()
                        .ResolveBatch(contexts =>
                        {
                            // only two results for three contexts.
                            var results = new ResolverResult[]
                            {
                                ResolverResult.Ok("Hello, Alice!"),
                                ResolverResult.Ok("Hello, Bob!")
                            };

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
                  "message": "Unexpected Execution Error",
                  "path": [
                    "users",
                    0,
                    "greeting"
                  ]
                },
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "users",
                    1,
                    "greeting"
                  ]
                },
                {
                  "message": "Unexpected Execution Error",
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
    public async Task BatchResolver_Should_Coalesce_Inside_Mutation_Payload()
    {
        // arrange
        // a batch field inside a single mutation payload selection set coalesces below
        // the serial root resolver into one batch invocation and completes.
        MutationPayloadQuery.BatchCallCount = 0;

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Name("Query").Field("noop").Resolve("noop"))
                .AddMutationType<MutationPayloadMutation>()
                .AddObjectType<PayloadItem>(d =>
                {
                    d.Field(i => i.Id);
                    d.Field("stock")
                        .Type<IntType>()
                        .ResolveBatch(contexts =>
                        {
                            MutationPayloadQuery.BatchCallCount++;
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var item = contexts[i].Parent<PayloadItem>();
                                results[i] = ResolverResult.Ok(item.Id * 100);
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doIt {
                            items {
                                id
                                stock
                            }
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, MutationPayloadQuery.BatchCallCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "doIt": {
                  "items": [
                    {
                      "id": 1,
                      "stock": 100
                    },
                    {
                      "id": 2,
                      "stock": 200
                    },
                    {
                      "id": 3,
                      "stock": 300
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ResolveBatchWith_Expression_Should_Resolve()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<User>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<User>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensions>(
                            t => t.GetGreeting(default!));
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task ResolveBatchWith_MemberInfo_Should_Resolve()
    {
        var method = typeof(UserExtensions).GetMethod(nameof(UserExtensions.GetGreeting))!;

        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<User>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<User>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith(method);
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task ResolveBatchWith_Expression_With_Service()
    {
        var result =
            await new ServiceCollection()
                .AddSingleton<GreetingService>()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<User>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<User>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensionsWithService>(
                            t => t.GetGreeting(default!, default!));
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task ResolveBatchWith_Expression_With_Argument()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<User>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<User>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensionsWithArgument>(
                            t => t.GetGreeting(default!, default!));
                })
                .ExecuteRequestAsync(
                    """
                    {
                        users {
                            name
                            greeting(prefix: "Hi")
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hi, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hi, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hi, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ResolveBatchWith_Expression_With_GlobalState()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<User>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<User>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensionsWithGlobalState>(
                            t => t.GetGreeting(default!, default!));
                })
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            {
                                users {
                                    name
                                    greeting
                                }
                            }
                            """)
                        .SetGlobalState("prefix", "Hey")
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hey, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hey, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hey, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ResolveBatchWith_Expression_With_CancellationToken()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<User>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddObjectType<User>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensionsWithCancellationToken>(
                            t => t.GetGreeting(default!, default));
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task BatchResolver_Annotated_Should_Resolve_Nested_Field()
    {
        // arrange & act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<AnnotatedQuery>()
                .AddTypeExtension<UserExtensions>()
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
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task BatchResolver_Annotated_With_Argument()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<AnnotatedQuery>()
                .AddTypeExtension<UserExtensionsWithArgument>()
                .ExecuteRequestAsync(
                    """
                    {
                        users {
                            name
                            greeting(prefix: "Hi")
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hi, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hi, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hi, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Annotated_With_Service()
    {
        var result =
            await new ServiceCollection()
                .AddSingleton<GreetingService>()
                .AddGraphQL()
                .AddQueryType<AnnotatedQuery>()
                .AddTypeExtension<UserExtensionsWithService>()
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task BatchResolver_Annotated_With_GlobalState()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<AnnotatedQuery>()
                .AddTypeExtension<UserExtensionsWithGlobalState>()
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            {
                                users {
                                    name
                                    greeting
                                }
                            }
                            """)
                        .SetGlobalState("prefix", "Hey")
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hey, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hey, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hey, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Annotated_With_ScopedState()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<User>>>()
                        .Resolve(ctx =>
                        {
                            ctx.ScopedContextData = ctx.ScopedContextData.SetItem("suffix", "!!!");
                            return new List<User>
                            {
                                new(1, "Alice"),
                                new(2, "Bob"),
                                new(3, "Charlie")
                            };
                        });
                })
                .AddTypeExtension<UserExtensionsWithScopedState>()
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!!!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!!!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hello, Charlie!!!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Annotated_With_CancellationToken()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<AnnotatedQuery>()
                .AddTypeExtension<UserExtensionsWithCancellationToken>()
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task BatchResolver_Interface_Inherited_By_ObjectType()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<InterfaceType<IUser>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddInterfaceType<IUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensions>(
                            t => t.GetGreeting(default!));
                })
                .AddObjectType<User>(d => d.Implements<InterfaceType<IUser>>())
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task BatchResolver_Interface_With_Argument()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<InterfaceType<IUser>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddInterfaceType<IUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensionsWithArgument>(
                            t => t.GetGreeting(default!, default!));
                })
                .AddObjectType<User>(d => d.Implements<InterfaceType<IUser>>())
                .ExecuteRequestAsync(
                    """
                    {
                        users {
                            name
                            greeting(prefix: "Hi")
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hi, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hi, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hi, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Interface_With_Service()
    {
        var result =
            await new ServiceCollection()
                .AddSingleton<GreetingService>()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<InterfaceType<IUser>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddInterfaceType<IUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensionsWithService>(
                            t => t.GetGreeting(default!, default!));
                })
                .AddObjectType<User>(d => d.Implements<InterfaceType<IUser>>())
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task BatchResolver_Interface_With_GlobalState()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<InterfaceType<IUser>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddInterfaceType<IUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensionsWithGlobalState>(
                            t => t.GetGreeting(default!, default!));
                })
                .AddObjectType<User>(d => d.Implements<InterfaceType<IUser>>())
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            {
                                users {
                                    name
                                    greeting
                                }
                            }
                            """)
                        .SetGlobalState("prefix", "Hey")
                        .Build(),
                    cancellationToken: TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hey, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hey, Bob!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hey, Charlie!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Interface_With_ScopedState()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<InterfaceType<IUser>>>()
                        .Resolve(ctx =>
                        {
                            ctx.ScopedContextData = ctx.ScopedContextData.SetItem("suffix", "!!!");
                            return new List<User>
                            {
                                new(1, "Alice"),
                                new(2, "Bob"),
                                new(3, "Charlie")
                            };
                        });
                })
                .AddInterfaceType<IUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensionsWithScopedState>(
                            t => t.GetGreeting(default!, default!));
                })
                .AddObjectType<User>(d => d.Implements<InterfaceType<IUser>>())
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!!!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!!!"
                  },
                  {
                    "name": "Charlie",
                    "greeting": "Hello, Charlie!!!"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Interface_With_CancellationToken()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<InterfaceType<IUser>>>()
                        .Resolve(new List<User>
                        {
                            new(1, "Alice"),
                            new(2, "Bob"),
                            new(3, "Charlie")
                        });
                })
                .AddInterfaceType<IUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .ResolveBatchWith<UserExtensionsWithCancellationToken>(
                            t => t.GetGreeting(default!, default));
                })
                .AddObjectType<User>(d => d.Implements<InterfaceType<IUser>>())
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

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "name": "Alice",
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "name": "Bob",
                    "greeting": "Hello, Bob!"
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
    public async Task BatchResolver_Should_Complete_When_Parent_Resolver_Is_Async()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("parents")
                        .Type<ListType<ObjectType<Parent>>>()
                        .Resolve(new List<Parent> { new(1), new(2) });
                })
                .AddObjectType<Parent>(d =>
                {
                    d.Field("children")
                        .Type<ListType<ObjectType<Child>>>()
                        .Resolve(async ctx =>
                        {
                            await Task.Delay(25, ctx.RequestAborted);
                            var parent = ctx.Parent<Parent>();
                            return new List<Child>
                            {
                                new(parent.Id * 10 + 1),
                                new(parent.Id * 10 + 2)
                            };
                        });
                })
                .AddObjectType<Child>(d =>
                {
                    d.Field(c => c.Id);
                    d.Field("computed")
                        .Type<StringType>()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var child = contexts[i].Parent<Child>();
                                results[i] = ResolverResult.Ok($"c{child.Id}");
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var resultTask = executor.ExecuteAsync(
            "{ parents { children { id computed } } }",
            TestContext.Current.CancellationToken);
        var result = await resultTask.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "parents": [
                  {
                    "children": [
                      {
                        "id": 11,
                        "computed": "c11"
                      },
                      {
                        "id": 12,
                        "computed": "c12"
                      }
                    ]
                  },
                  {
                    "children": [
                      {
                        "id": 21,
                        "computed": "c21"
                      },
                      {
                        "id": 22,
                        "computed": "c22"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Batch_Should_Complete_When_Async_Parents_Are_Nested_Three_Levels()
    {
        // arrange
        var batchSizes = new List<int>();
        var executor = await CreateAsyncParentBuilder(2)
            .AddObjectType<Child>(d =>
            {
                d.Field("grandchildren")
                    .Type<ListType<ObjectType<GrandChild>>>()
                    .Resolve(async ctx =>
                    {
                        await Task.Delay(25, ctx.RequestAborted);
                        var child = ctx.Parent<Child>();
                        return new List<GrandChild> { new(child.Id * 10 + 1), new(child.Id * 10 + 2) };
                    });
            })
            .AddObjectType<GrandChild>(d =>
            {
                d.Field(c => c.Id);
                d.Field("computed").Type<StringType>().ResolveBatch(contexts =>
                {
                    batchSizes.Add(contexts.Count);
                    return new ValueTask<IReadOnlyList<ResolverResult>>(
                        contexts.Select(c => ResolverResult.Ok($"g{c.Parent<GrandChild>().Id}")).ToArray());
                });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
                "{ parents { children { grandchildren { id computed } } } }",
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(batchSizes, "Batch sizes").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Should_Complete_When_There_Are_Fifty_Async_Parents()
    {
        // arrange
        var batchSizes = new List<int>();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("parents")
                .Type<ListType<ObjectType<Parent>>>()
                .Resolve(Enumerable.Range(1, 50).Select(i => new Parent(i)).ToList()))
            .AddObjectType<Parent>(d => d.Field("children")
                .Type<ListType<ObjectType<Child>>>()
                .Resolve(async ctx =>
                {
                    await Task.Delay(ctx.Parent<Parent>().Id % 29 + 1, ctx.RequestAborted);
                    return new List<Child> { new(ctx.Parent<Parent>().Id) };
                }))
            .AddObjectType<Child>(d =>
            {
                d.Field(c => c.Id);
                d.Field("computed").Type<StringType>().ResolveBatch(contexts =>
                {
                    batchSizes.Add(contexts.Count);
                    return ComputeChildrenAsync(contexts);
                });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ parents { children { id computed } } }",
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(batchSizes, "Batch sizes").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Should_Complete_When_Parent_List_Itself_Is_Async()
    {
        // arrange
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("parents")
                .Type<ListType<ObjectType<Parent>>>()
                .Resolve(async ctx =>
                {
                    await Task.Delay(25, ctx.RequestAborted);
                    return new List<Parent> { new(1), new(2) };
                }))
            .AddObjectType<Parent>(ConfigureAsyncChildren)
            .AddObjectType<Child>(ConfigureComputedChild)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ parents { children { id computed } } }",
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Should_Complete_When_Single_Async_Parent_Has_Slow_Sibling_Root_Field()
    {
        // arrange
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("parent").Type<ObjectType<Parent>>().Resolve(new Parent(1));
                ConfigureSlowSibling(d);
            })
            .AddObjectType<Parent>(ConfigureAsyncChildren)
            .AddObjectType<Child>(ConfigureComputedChild)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ parent { children { id computed } } slow }",
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Should_Complete_When_Async_Children_Are_Under_Defer()
    {
        // arrange
        var executor = await CreateAsyncParentBuilder(2)
            .AddObjectType<Child>(ConfigureComputedChild)
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
                "{ parents { id ... @defer { children { id computed } } } }",
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var (payloads, hasNext) = await DrainBatchResultsAsync(result)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        var snapshot = new Snapshot().Add(hasNext, "Stream continuation");
        // Deferred siblings can complete in either order.
        foreach (var payload in payloads.OrderBy(p => p["incremental"]?[0]?["id"]?.GetValue<string>(),
            StringComparer.Ordinal))
        {
            snapshot.Add(payload.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                "Payload", "json");
        }

        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Should_Complete_When_One_Async_Parent_Resolver_Throws()
    {
        // arrange
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("parents")
                .Type<ListType<ObjectType<Parent>>>()
                .Resolve(new List<Parent> { new(1), new(2) }))
            .AddObjectType<Parent>(d =>
            {
                d.Field(p => p.Id);
                d.Field("children").Type<ListType<ObjectType<Child>>>().Resolve(async ctx =>
                {
                    var parent = ctx.Parent<Parent>();
                    await Task.Delay(parent.Id == 2 ? 10 : 100, ctx.RequestAborted);
                    if (parent.Id == 2)
                    {
                        throw new InvalidOperationException("boom");
                    }

                    return CreateChildren(parent.Id);
                });
            })
            .AddObjectType<Child>(ConfigureComputedChild)
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = false)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ parents { id children { id computed } } }",
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Should_Complete_When_Parents_And_Batch_Resolver_Await_DataLoaders()
    {
        // arrange
        var batchSizes = new List<int>();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddDataLoader<ChildrenByParentDataLoader>()
            .AddDataLoader<ChildNameDataLoader>()
            .AddQueryType(d => d.Name("Query").Field("parents")
                .Type<ListType<ObjectType<Parent>>>()
                .Resolve(new List<Parent> { new(1), new(2), new(3) }))
            .AddObjectType<Parent>(d => d.Field("children")
                .Type<ListType<ObjectType<Child>>>()
                .Resolve(async ctx => await ctx.DataLoader<ChildrenByParentDataLoader>()
                    .LoadRequiredAsync(ctx.Parent<Parent>().Id, ctx.RequestAborted)))
            .AddObjectType<Child>(d =>
            {
                d.Field(c => c.Id);
                d.Field("computed").Type<StringType>().ResolveBatch(async contexts =>
                {
                    batchSizes.Add(contexts.Count);
                    var names = await contexts[0].DataLoader<ChildNameDataLoader>()
                        .LoadRequiredAsync(contexts.Select(c => c.Parent<Child>().Id).ToArray(),
                            contexts[0].RequestAborted);
                    return names.Select(name => ResolverResult.Ok(name)).ToArray();
                });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ parents { children { id computed } } }",
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(batchSizes, "Batch sizes").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Should_Complete_When_Two_Sibling_Batch_Fields_Are_Under_Async_Parents()
    {
        // arrange
        var batchSizesA = new List<int>();
        var batchSizesB = new List<int>();
        var executor = await CreateAsyncParentBuilder(2)
            .AddObjectType<Child>(d =>
            {
                d.Field(c => c.Id);
                d.Field("computedA").Type<StringType>().ResolveBatch(contexts =>
                {
                    batchSizesA.Add(contexts.Count);
                    return ComputeChildrenAsync(contexts, "a");
                });
                d.Field("computedB").Type<StringType>().ResolveBatch(async contexts =>
                {
                    batchSizesB.Add(contexts.Count);
                    await Task.Delay(25, contexts[0].RequestAborted);
                    return await ComputeChildrenAsync(contexts, "b");
                });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ parents { children { id computedA computedB } } }",
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(batchSizesA, "Batch A sizes")
            .Add(batchSizesB, "Batch B sizes").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Should_Complete_When_Batch_Result_Has_Batch_Field_And_Slow_Sibling_Runs()
    {
        // arrange
        var childrenBatchSizes = new List<int>();
        var computedBatchSizes = new List<int>();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("parents").Type<ListType<ObjectType<Parent>>>()
                    .Resolve(new List<Parent> { new(1), new(2) });
                ConfigureSlowSibling(d);
            })
            .AddObjectType<Parent>(d => d.Field("children")
                .Type<ListType<ObjectType<Child>>>()
                .ResolveBatch(async contexts =>
                {
                    childrenBatchSizes.Add(contexts.Count);
                    await Task.Delay(25, contexts[0].RequestAborted);
                    return contexts.Select(c => ResolverResult.Ok(CreateChildren(c.Parent<Parent>().Id)))
                        .ToArray();
                }))
            .AddObjectType<Child>(d =>
            {
                d.Field(c => c.Id);
                d.Field("computed").Type<StringType>().ResolveBatch(contexts =>
                {
                    computedBatchSizes.Add(contexts.Count);
                    return ComputeChildrenAsync(contexts);
                });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ parents { children { id computed } } slow }",
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(childrenBatchSizes, "Children batch sizes")
            .Add(computedBatchSizes, "Computed batch sizes").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Should_Complete_When_Sync_Parent_List_Has_Batch_Field_And_Slow_Sibling_Runs()
    {
        // arrange
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("children").Type<ListType<ObjectType<Child>>>()
                    .Resolve(new List<Child> { new(1), new(2) });
                ConfigureSlowSibling(d);
            })
            .AddObjectType<Child>(ConfigureComputedChild)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ children { id computed } slow }",
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "children": [
                  {
                    "id": 1,
                    "computed": "c1"
                  },
                  {
                    "id": 2,
                    "computed": "c2"
                  }
                ],
                "slow": "slow"
              }
            }
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Batch_Should_Complete_When_Serial_Mutation_Parents_Have_Async_Children(bool aliasSameField)
    {
        // arrange
        var batchSizes = new List<int>();
        var events = new List<string>();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("ping").Resolve("pong"))
            .AddMutationType(d =>
            {
                d.Name("Mutation");
                foreach (var name in new[] { "createParent", "cloneParent" })
                {
                    d.Field(name).Argument("id", a => a.Type<NonNullType<IntType>>())
                        .Type<ObjectType<Parent>>().Resolve(async ctx =>
                        {
                            var id = ctx.ArgumentValue<int>("id");
                            events.Add($"mutation-{id}-start");
                            await Task.Delay(10, ctx.RequestAborted);
                            return new Parent(id);
                        });
                }
            })
            .AddObjectType<Parent>(ConfigureAsyncChildren)
            .AddObjectType<Child>(d =>
            {
                d.Field(c => c.Id);
                d.Field("computed").Type<StringType>().ResolveBatch(async contexts =>
                {
                    batchSizes.Add(contexts.Count);
                    var results = await ComputeChildrenAsync(contexts);
                    events.Add($"batch-{contexts[0].Parent<Child>().Id / 10}-complete");
                    return results;
                });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var secondField = aliasSameField ? "createParent" : "cloneParent";

        // act
        var result = await executor.ExecuteAsync(
                $$"""
                mutation {
                  a: createParent(id: 1) { id children { id computed } }
                  b: {{secondField}}(id: 2) { id children { id computed } }
                }
                """, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        new Snapshot(postFix: aliasSameField.ToString()).Add(result, "Result").Add(batchSizes, "Batch sizes")
            .Add(events, "Events")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Should_Group_One_Selection_When_Parents_Come_From_Different_Variable_Sets()
    {
        // arrange
        var batchSizes = new List<int>();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("parent")
                .Argument("id", a => a.Type<NonNullType<IntType>>())
                .Type<ObjectType<Parent>>()
                .Resolve(ctx => new Parent(ctx.ArgumentValue<int>("id"))))
            .AddObjectType<Parent>(ConfigureAsyncChildren)
            .AddObjectType<Child>(d =>
            {
                d.Field(c => c.Id);
                d.Field("computed").Type<StringType>().ResolveBatch(contexts =>
                {
                    batchSizes.Add(contexts.Count);
                    return ComputeChildrenAsync(contexts);
                });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument("query($id: Int!) { parent(id: $id) { id children { id computed } } }")
                    .SetVariableValues(new List<IReadOnlyDictionary<string, object?>>
                    {
                        new Dictionary<string, object?> { ["id"] = 1 },
                        new Dictionary<string, object?> { ["id"] = 2 }
                    })
                    .Build(), TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Equal(2, batch.Results.Count);
        new Snapshot().Add(batch.Results[0], "Set 0").Add(batch.Results[1], "Set 1")
            .Add(batchSizes, "Batch sizes").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Execution_Should_Not_Hang_When_Request_Is_Cancelled_While_Async_Children_Run()
    {
        // arrange
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("parents")
                .Type<ListType<ObjectType<Parent>>>()
                .Resolve(new List<Parent> { new(1), new(2) }))
            .AddObjectType<Parent>(d => d.Field("children")
                .Type<ListType<ObjectType<Child>>>().Resolve(async ctx =>
                {
                    var parent = ctx.Parent<Parent>();
                    await Task.Delay(parent.Id == 1 ? 10 : 2000, ctx.RequestAborted);
                    return CreateChildren(parent.Id);
                }))
            .AddObjectType<Child>(ConfigureComputedChild)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(200);

        // act
        var result = await executor.ExecuteAsync("{ parents { children { id computed } } }", cts.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The GraphQL request execution was canceled.",
                  "extensions": {
                    "code": "HC0049"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Batch_Should_Complete_When_Root_Batch_Field_Has_Sibling()
    {
        // arrange
        var executor = await CreateRootBatchBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ computed ping }", TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "computed": "root",
                "ping": "pong"
              }
            }
            """);
    }

    [Fact]
    public async Task Batch_Should_Complete_When_Root_Batch_Field_Is_Only_Selection()
    {
        // arrange
        var executor = await CreateRootBatchBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ computed }", TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "computed": "root"
              }
            }
            """);
    }

    private static Configuration.IRequestExecutorBuilder CreateRootBatchBuilder()
        => new ServiceCollection().AddGraphQL().AddQueryType(d =>
        {
            d.Name("Query");
            d.Field("ping").Resolve("pong");
            d.Field("computed").Type<StringType>().ResolveBatch(contexts =>
                new ValueTask<IReadOnlyList<ResolverResult>>(
                    contexts.Select(_ => ResolverResult.Ok("root")).ToArray()));
        });

    private static Configuration.IRequestExecutorBuilder CreateAsyncParentBuilder(int count)
        => new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("parents")
                .Type<ListType<ObjectType<Parent>>>()
                .Resolve(Enumerable.Range(1, count).Select(i => new Parent(i)).ToList()))
            .AddObjectType<Parent>(ConfigureAsyncChildren);

    private static void ConfigureAsyncChildren(IObjectTypeDescriptor<Parent> descriptor)
    {
        descriptor.Field(p => p.Id);
        descriptor.Field("children").Type<ListType<ObjectType<Child>>>().Resolve(async ctx =>
        {
            await Task.Delay(25, ctx.RequestAborted);
            return CreateChildren(ctx.Parent<Parent>().Id);
        });
    }

    private static void ConfigureComputedChild(IObjectTypeDescriptor<Child> descriptor)
    {
        descriptor.Field(c => c.Id);
        descriptor.Field("computed").Type<StringType>().ResolveBatch(ComputeChildrenAsync);
    }

    private static void ConfigureSlowSibling(IObjectTypeDescriptor descriptor)
    {
        descriptor.Field("slow").Type<StringType>().Resolve(async ctx =>
        {
            await Task.Delay(300, ctx.RequestAborted);
            return "slow";
        });
    }

    private static List<Child> CreateChildren(int parentId)
        => [new(parentId * 10 + 1), new(parentId * 10 + 2)];

    private static ValueTask<IReadOnlyList<ResolverResult>> ComputeChildrenAsync(
        IReadOnlyList<IResolverContext> contexts)
        => ComputeChildrenAsync(contexts, "c");

    private static ValueTask<IReadOnlyList<ResolverResult>> ComputeChildrenAsync(
        IReadOnlyList<IResolverContext> contexts, string prefix)
        => new(contexts.Select(c => ResolverResult.Ok($"{prefix}{c.Parent<Child>().Id}")).ToArray());

    private static async Task<(List<JsonObject> Payloads, List<bool> HasNext)> DrainBatchResultsAsync(
        IExecutionResult result)
    {
        var payloads = new List<JsonObject>();
        var hasNext = new List<bool>();
        await using var stream = result.ExpectResponseStream();
        await foreach (var payload in stream.ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            var node = JsonNode.Parse(payload.ToJson())!.AsObject();
            hasNext.Add(node["hasNext"]!.GetValue<bool>());
            node.Remove("hasNext");
            payloads.Add(node);
        }

        return (payloads, hasNext);
    }

    public record GrandChild(int Id);

    public class ChildrenByParentDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
        : BatchDataLoader<int, List<Child>>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, List<Child>>> LoadBatchAsync(
            IReadOnlyList<int> keys, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            return keys.ToDictionary(k => k, CreateChildren);
        }
    }

    public class ChildNameDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
        : BatchDataLoader<int, string>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, string>> LoadBatchAsync(
            IReadOnlyList<int> keys, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            return keys.ToDictionary(k => k, k => $"name{k}");
        }
    }

    public interface IUser
    {
        string Name { get; }
    }

    public record User(int Id, string Name) : IUser;

    public record Product(int Id, string Name);

    public record Parent(int Id);

    public record Child(int Id);

    public record PayloadItem(int Id);

    public record DoItPayload(List<PayloadItem> Items);

    public static class MutationPayloadQuery
    {
        public static int BatchCallCount { get; set; }
    }

    public class MutationPayloadMutation
    {
        public DoItPayload DoIt()
            => new(
            [
                new PayloadItem(1),
                new PayloadItem(2),
                new PayloadItem(3)
            ]);
    }

    public class ProductByIdQuery
    {
        private static readonly Dictionary<int, Product> s_products = new()
        {
            { 1, new Product(1, "Product 1") },
            { 2, new Product(2, "Product 2") }
        };

        public static int BatchCallCount { get; set; }

        [BatchResolver]
        public List<Product?> GetProductById(List<int> id)
        {
            BatchCallCount++;
            return id.Select(t => s_products.GetValueOrDefault(t)).ToList();
        }
    }

    public class AnnotatedQuery
    {
        public List<User> GetUsers()
            =>
            [
                new User(1, "Alice"),
                new User(2, "Bob"),
                new User(3, "Charlie")
            ];
    }

    public class GreetingService
    {
        public string Greet(string name) => $"Hello, {name}!";
    }

    [ExtendObjectType<User>]
    public class UserExtensions
    {
        [BatchResolver]
        public List<string> GetGreeting([Parent] List<User> users)
        {
            var result = new List<string>();

            foreach (var user in users)
            {
                result.Add($"Hello, {user.Name}!");
            }

            return result;
        }
    }

    [ExtendObjectType<User>]
    public class UserExtensionsWithArgument
    {
        [BatchResolver]
        public List<string> GetGreeting(
            [Parent] List<User> users,
            List<string> prefix)
        {
            var result = new List<string>();

            for (var i = 0; i < users.Count; i++)
            {
                result.Add($"{prefix[i]}, {users[i].Name}!");
            }

            return result;
        }
    }

    [ExtendObjectType<User>]
    public class UserExtensionsWithService
    {
        [BatchResolver]
        public List<string> GetGreeting(
            [Parent] List<User> users,
            [Service] GreetingService greetingService)
        {
            var result = new List<string>();

            foreach (var user in users)
            {
                result.Add(greetingService.Greet(user.Name));
            }

            return result;
        }
    }

    [ExtendObjectType<User>]
    public class UserExtensionsWithGlobalState
    {
        [BatchResolver]
        public List<string> GetGreeting(
            [Parent] List<User> users,
            [GlobalState("prefix")] string prefix)
        {
            var result = new List<string>();

            foreach (var user in users)
            {
                result.Add($"{prefix}, {user.Name}!");
            }

            return result;
        }
    }

    [ExtendObjectType<User>]
    public class UserExtensionsWithScopedState
    {
        [BatchResolver]
        public List<string> GetGreeting(
            [Parent] List<User> users,
            [ScopedState("suffix")] string suffix)
        {
            var result = new List<string>();

            foreach (var user in users)
            {
                result.Add($"Hello, {user.Name}{suffix}");
            }

            return result;
        }
    }

    [ExtendObjectType<User>]
    public class UserExtensionsWithCancellationToken
    {
        [BatchResolver]
        public List<string> GetGreeting(
            [Parent] List<User> users,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = new List<string>();

            foreach (var user in users)
            {
                result.Add($"Hello, {user.Name}!");
            }

            return result;
        }
    }
}
