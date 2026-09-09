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
                    """);

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
    public async Task BatchResolver_Should_Batch_Aliased_Field_Arguments()
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
                    """);

        // assert
        Assert.Equal(1, ProductByIdQuery.BatchCallCount);
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
    public async Task BatchResolver_Should_Batch_Aliased_Field_Variable_Arguments()
    {
        // arrange
        // exercises the per-context CoerceArguments-with-variables path that the
        // literal-only test BatchResolver_Should_Batch_Aliased_Field_Arguments skips.
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
                        .Build());

        // assert
        Assert.Equal(1, ProductByIdQuery.BatchCallCount);
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
    public async Task BatchResolver_Should_Coalesce_Remaining_Siblings_When_One_Is_Skipped()
    {
        // arrange
        // a skipped alias must drop out of the batch while the included sibling
        // still coalesces into a single batch invocation.
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
                        .Build());

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
                    """);

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
                        .Build());

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
                    """);

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
                    """);

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
                    """);

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
                    """);

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
                    """);

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
                    """);

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
                    """);

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
                        .Build());

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
                    """);

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
                    """);

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
                    """);

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
                    """);

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
                        .Build());

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
                    """);

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
                    """);

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
                    """);

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
                    """);

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
                    """);

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
                        .Build());

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
                    """);

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
                    """);

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
        // The schema is built before the request so the build time is outside the
        // hang-guard window.
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
                            await Task.Delay(25);
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
                .BuildRequestExecutorAsync();

        // act
        // A CancellationToken passed to ExecuteAsync does not unblock the hang. The work loop
        // checks the token, but the scheduler pause awaits its signal without a cancellation
        // registration and so never observes it, so the guard must be WaitAsync at the test
        // level. See https://github.com/ChilliCream/graphql-platform/issues/9892.
        var resultTask = executor.ExecuteAsync("{ parents { children { id computed } } }");
        var result = await resultTask.WaitAsync(TimeSpan.FromSeconds(10));

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
