using System.Collections.Concurrent;
using System.Text.Json;
using GreenDonut;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

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
    public async Task ExecuteAsync_Should_ResolveBatchField_When_SiblingRootFieldCompletesLast()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await ExecuteAsync(
            executor,
            OperationRequest.FromSourceText(
                """
                {
                    users {
                        greeting
                    }
                    hello
                }
                """));

        // assert
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
                ],
                "hello": "world"
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ResolveBatchField_When_ParentFieldIsSelectedTwice()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await ExecuteAsync(
            executor,
            OperationRequest.FromSourceText(
                """
                {
                    a: users {
                        greeting
                    }
                    b: users {
                        name
                    }
                }
                """));

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": [
                  {
                    "greeting": "Hello, Alice!"
                  },
                  {
                    "greeting": "Hello, Bob!"
                  },
                  {
                    "greeting": "Hello, Charlie!"
                  }
                ],
                "b": [
                  {
                    "name": "Alice"
                  },
                  {
                    "name": "Bob"
                  },
                  {
                    "name": "Charlie"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ResolveBatchField_When_ParentsAreResolvedAsynchronously()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await ExecuteAsync(
            executor,
            OperationRequest.FromSourceText(
                """
                {
                    users {
                        friend {
                            greeting
                        }
                    }
                }
                """));

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "friend": {
                      "greeting": "Hello, Friend of Alice!"
                    }
                  },
                  {
                    "friend": {
                      "greeting": "Hello, Friend of Bob!"
                    }
                  },
                  {
                    "friend": {
                      "greeting": "Hello, Friend of Charlie!"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ResolveBatchField_When_AllRootFieldsAreBatchFields()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await ExecuteAsync(
            executor,
            OperationRequest.FromSourceText(
                """
                {
                    motto
                }
                """));

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "motto": "Keep it simple."
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ResolveBatchFieldPerVariableSet_When_RequestHasVariableBatch()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await ExecuteAsync(
            executor,
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($prefix: String!) {
                        users {
                            greetingWithPrefix(prefix: $prefix)
                        }
                    }
                    """)
                .SetVariableValues(
                    """
                    [
                        { "prefix": "Hi" },
                        { "prefix": "Hey" }
                    ]
                    """)
                .Build());

        // assert
        result.ExpectOperationResultBatch().Results.MatchInlineSnapshots(
        [
            """
            {
              "variableIndex": 0,
              "data": {
                "users": [
                  {
                    "greetingWithPrefix": "Hi, Alice!"
                  },
                  {
                    "greetingWithPrefix": "Hi, Bob!"
                  },
                  {
                    "greetingWithPrefix": "Hi, Charlie!"
                  }
                ]
              }
            }
            """,
            """
            {
              "variableIndex": 1,
              "data": {
                "users": [
                  {
                    "greetingWithPrefix": "Hey, Alice!"
                  },
                  {
                    "greetingWithPrefix": "Hey, Bob!"
                  },
                  {
                    "greetingWithPrefix": "Hey, Charlie!"
                  }
                ]
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ResolveDeferredParentsInOneBatch_When_BatchFieldIsDeferredInList()
    {
        // arrange
        var batchSizes = new ConcurrentQueue<int>();
        var dataLoaders = new List<UserNameByIdDataLoader>();
        var executor = await CreateCountingExecutorAsync(batchSizes, dataLoaders);

        // act
        var result = await ExecuteAsync(
            executor,
            OperationRequest.FromSourceText(
                """
                {
                    users {
                        name
                        ... @defer {
                            friend {
                                countedGreeting
                            }
                        }
                    }
                }
                """));
        var payloads = await ReadAllAsync(result);

        // assert
        Assert.Equal(
            ["Hello, Friend of Alice!", "Hello, Friend of Bob!", "Hello, Friend of Charlie!"],
            GetFieldValues(payloads, "countedGreeting"));
        Assert.Equal([3], batchSizes);
    }

    [Fact]
    public async Task ExecuteAsync_Should_BatchDeferredParentsPerVariableSet_When_RequestHasVariableBatch()
    {
        // arrange
        var batchSizes = new ConcurrentQueue<int>();
        var dataLoaders = new List<UserNameByIdDataLoader>();
        var executor = await CreateCountingExecutorAsync(batchSizes, dataLoaders);

        // act
        var result = await ExecuteAsync(
            executor,
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($prefix: String!) {
                        users {
                            name
                            ... @defer {
                                friend {
                                    countedGreetingWithPrefix(prefix: $prefix)
                                }
                            }
                        }
                    }
                    """)
                .SetVariableValues(
                    """
                    [
                        { "prefix": "Hi" },
                        { "prefix": "Hey" }
                    ]
                    """)
                .Build());
        var results = result.ExpectOperationResultBatch().Results;
        var first = await ReadAllAsync(results[0]);
        var second = await ReadAllAsync(results[1]);

        // assert
        Assert.Equal(
            ["Hi, Friend of Alice!", "Hi, Friend of Bob!", "Hi, Friend of Charlie!"],
            GetFieldValues(first, "countedGreetingWithPrefix"));
        Assert.Equal(
            ["Hey, Friend of Alice!", "Hey, Friend of Bob!", "Hey, Friend of Charlie!"],
            GetFieldValues(second, "countedGreetingWithPrefix"));
        Assert.Equal([3, 3], batchSizes);
        Assert.Equal([[101, 102, 103]], Assert.Single(dataLoaders).Loads);
    }

    [Fact]
    public async Task ExecuteAsync_Should_BatchEachVariableSetWithItsOwnVariables_When_RequestHasVariableBatch()
    {
        // arrange
        var batchSizes = new ConcurrentQueue<int>();
        var dataLoaders = new List<UserNameByIdDataLoader>();
        var executor = await CreateCountingExecutorAsync(batchSizes, dataLoaders);

        // act
        var result = await ExecuteAsync(
            executor,
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($prefix: String!) {
                        users {
                            countedGreetingWithPrefix(prefix: $prefix)
                        }
                    }
                    """)
                .SetVariableValues(
                    """
                    [
                        { "prefix": "Hi" },
                        { "prefix": "Hey" },
                        { "prefix": "Hello" }
                    ]
                    """)
                .Build());

        // assert
        result.ExpectOperationResultBatch().Results.MatchInlineSnapshots(
        [
            """
            {
              "variableIndex": 0,
              "data": {
                "users": [
                  {
                    "countedGreetingWithPrefix": "Hi, Alice!"
                  },
                  {
                    "countedGreetingWithPrefix": "Hi, Bob!"
                  },
                  {
                    "countedGreetingWithPrefix": "Hi, Charlie!"
                  }
                ]
              }
            }
            """,
            """
            {
              "variableIndex": 1,
              "data": {
                "users": [
                  {
                    "countedGreetingWithPrefix": "Hey, Alice!"
                  },
                  {
                    "countedGreetingWithPrefix": "Hey, Bob!"
                  },
                  {
                    "countedGreetingWithPrefix": "Hey, Charlie!"
                  }
                ]
              }
            }
            """,
            """
            {
              "variableIndex": 2,
              "data": {
                "users": [
                  {
                    "countedGreetingWithPrefix": "Hello, Alice!"
                  },
                  {
                    "countedGreetingWithPrefix": "Hello, Bob!"
                  },
                  {
                    "countedGreetingWithPrefix": "Hello, Charlie!"
                  }
                ]
              }
            }
            """
        ]);
        Assert.Equal([3, 3, 3], batchSizes);
        Assert.Equal([[1, 2, 3]], Assert.Single(dataLoaders).Loads);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ResolveAllParentsInOneBatch_When_BatchFieldUsesDataLoader()
    {
        // arrange
        var batchSizes = new ConcurrentQueue<int>();
        var dataLoaders = new List<UserNameByIdDataLoader>();
        var executor = await CreateCountingExecutorAsync(batchSizes, dataLoaders);

        // act
        var result = await ExecuteAsync(
            executor,
            OperationRequest.FromSourceText(
                """
                {
                    users {
                        countedGreetingWithPrefix(prefix: "Hi")
                    }
                }
                """));

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "users": [
                  {
                    "countedGreetingWithPrefix": "Hi, Alice!"
                  },
                  {
                    "countedGreetingWithPrefix": "Hi, Bob!"
                  },
                  {
                    "countedGreetingWithPrefix": "Hi, Charlie!"
                  }
                ]
              }
            }
            """);
        Assert.Equal([3], batchSizes);
        Assert.Equal([[1, 2, 3]], Assert.Single(dataLoaders).Loads);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ExecuteAsync_Should_BatchAllDeferredParents_When_ParentsExceedSchedulerBuffer(
        bool resolveAsynchronously,
        bool nestedDefer)
    {
        // arrange
        var count = (Environment.ProcessorCount * 2) + 1;
        var users = Enumerable.Range(0, count).Select(i => new User(i, $"User {i:D6}")).ToArray();
        var batchSizes = new ConcurrentQueue<int>();
        var query = nestedDefer
            ? "{ users { ... @defer { friend { ... @defer { friend { greeting } } } } } }"
            : "{ users { ... @defer { friend { greeting } } } }";
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o => o.EnableDefer = true)
            .AddQueryType(d => d
                .Name("Query")
                .Field("users")
                .Type<ListType<ObjectType<User>>>()
                .Resolve(users))
            .AddObjectType<User>(d =>
            {
                d.Field("friend")
                    .Type<ObjectType<User>>()
                    .Resolve(async context =>
                    {
                        var user = context.Parent<User>();

                        if (resolveAsynchronously && user.Id % 2 == 0)
                        {
                            await Task.Yield();
                        }

                        return user;
                    });
                d.Field("greeting")
                    .Type<StringType>()
                    .ResolveBatch(contexts =>
                    {
                        batchSizes.Enqueue(contexts.Count);
                        IReadOnlyList<ResolverResult> results = contexts
                            .Select(c => ResolverResult.Ok(c.Parent<User>().Name))
                            .ToArray();
                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            OperationRequest.FromSourceText(query));
        var payloads = await ReadAllAsync(result);

        // assert
        Assert.Equal([count], batchSizes);
        Assert.Equal(users.Select(u => u.Name), GetFieldValues(payloads, "greeting"));
    }

    [Fact]
    public async Task ExecuteAsync_Should_RetainDeferredContext_When_BatchCleanupIsAwaiting()
    {
        // arrange
        var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var rootGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupSchema = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        TrackingBatchResolverTaskPool taskPool = null!;
        var executor = await new ServiceCollection()
            .AddSingleton<ObjectPool<BatchResolverTask>>(sp => taskPool = new TrackingBatchResolverTaskPool(
                sp.GetRequiredService<ObjectPool<ResolverTask>>(),
                sp.GetRequiredService<ObjectPool<Dictionary<string, ArgumentValue>>>()))
            .AddGraphQL()
            .ModifyOptions(o => o.EnableDefer = true)
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("users")
                    .Type<ListType<ObjectType<User>>>()
                    .Resolve(new[] { new User(1, "Alice"), new User(2, "Bob") });
                d.Field("hold")
                    .Type<StringType>()
                    .Resolve(async _ =>
                    {
                        await rootGate.Task;
                        return "done";
                    });
            })
            .AddObjectType<User>(d =>
            {
                d.Field("friend")
                    .Type<ObjectType<User>>()
                    .Resolve(context => context.Parent<User>());
                d.Field("greeting")
                    .Type<StringType>()
                    .ResolveBatch(contexts =>
                    {
                        var context = (IMiddlewareContext)contexts[0];
                        context.RegisterForCleanup(async () =>
                        {
                            cleanupEntered.SetResult();
                            await cleanupGate.Task;

                            try
                            {
                                cleanupSchema.SetResult(context.Schema.Name);
                            }
                            catch (Exception ex)
                            {
                                cleanupSchema.SetException(ex);
                            }
                        });
                        IReadOnlyList<ResolverResult> results = contexts
                            .Select(c => ResolverResult.Ok(c.Parent<User>().Name))
                            .ToArray();
                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var execution = ExecuteAsync(
            executor,
            OperationRequest.FromSourceText(
                "{ users { ... @defer { friend { greeting } } } hold }"));
        bool retained;
        OperationContext operationContext;

        try
        {
            await cleanupEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            operationContext = (OperationContext)taskPool.LastTask.Context;
            retained = operationContext.HasOutstandingTasks;
        }
        finally
        {
            cleanupGate.TrySetResult();
            rootGate.TrySetResult();
        }

        await using var result = await execution;
        var payloads = await ReadAllAsync(result);
        await taskPool.Returned.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        Assert.True(retained);
        Assert.Equal(executor.Schema.Name, await cleanupSchema.Task);
        Assert.False(operationContext.HasOutstandingTasks);
        Assert.Equal(["Alice", "Bob"], GetFieldValues(payloads, "greeting"));
    }

    private sealed class TrackingBatchResolverTaskPool(
        ObjectPool<ResolverTask> resolverTaskPool,
        ObjectPool<Dictionary<string, ArgumentValue>> argumentMapPool)
        : ObjectPool<BatchResolverTask>
    {
        public BatchResolverTask LastTask { get; private set; } = null!;

        public TaskCompletionSource Returned { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override BatchResolverTask Get()
            => LastTask = new BatchResolverTask(this, resolverTaskPool, argumentMapPool);

        public override void Return(BatchResolverTask obj)
        {
            obj.Reset();
            Returned.TrySetResult();
        }
    }

    private static async Task<IRequestExecutor> CreateCountingExecutorAsync(
        ConcurrentQueue<int> batchSizes,
        List<UserNameByIdDataLoader> dataLoaders)
        => await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(o => o.EnableDefer = true)
            .AddQueryType<AnnotatedQuery>()
            .AddTypeExtension<UserExtensionsWithFriend>()
            .AddTypeExtension(new ObjectTypeExtension<User>(d =>
            {
                d.Field("countedGreeting")
                    .Type<StringType>()
                    .ResolveBatch(contexts =>
                    {
                        batchSizes.Enqueue(contexts.Count);

                        var results = new ResolverResult[contexts.Count];

                        for (var i = 0; i < contexts.Count; i++)
                        {
                            results[i] = ResolverResult.Ok($"Hello, {contexts[i].Parent<User>().Name}!");
                        }

                        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                    });

                d.Field("countedGreetingWithPrefix")
                    .Argument("prefix", a => a.Type<NonNullType<StringType>>())
                    .Type<StringType>()
                    .ResolveBatch(async contexts =>
                    {
                        batchSizes.Enqueue(contexts.Count);

                        var names = await contexts[0]
                            .DataLoader<UserNameByIdDataLoader>()
                            .LoadRequiredAsync(
                                contexts.Select(c => c.Parent<User>().Id).ToList(),
                                contexts[0].RequestAborted);
                        var results = new ResolverResult[contexts.Count];

                        for (var i = 0; i < contexts.Count; i++)
                        {
                            var prefix = contexts[i].ArgumentValue<string>("prefix");
                            results[i] = ResolverResult.Ok($"{prefix}, {names[i]}!");
                        }

                        return results;
                    });
            }))
            .AddDataLoader(sp =>
            {
                var dataLoader = new UserNameByIdDataLoader(
                    sp.GetRequiredService<IBatchScheduler>(),
                    new DataLoaderOptions());
                dataLoaders.Add(dataLoader);
                return dataLoader;
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static async Task<List<string>> ReadAllAsync(IExecutionResult result)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var payloads = new List<string>();

        await Task.Run(
            async () =>
            {
                await foreach (var payload in result.ExpectResponseStream()
                    .ReadResultsAsync()
                    .WithCancellation(cancellationToken))
                {
                    payloads.Add(payload.ToJson());
                }
            },
            cancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

        return payloads;
    }

    private static List<string> GetFieldValues(List<string> payloads, string fieldName)
    {
        var values = new List<string>();

        foreach (var payload in payloads)
        {
            using var document = JsonDocument.Parse(payload);
            CollectFieldValues(document.RootElement, fieldName, values);
        }

        values.Sort(StringComparer.Ordinal);
        return values;

        static void CollectFieldValues(JsonElement element, string fieldName, List<string> values)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.NameEquals(fieldName))
                        {
                            values.Add(property.Value.GetString()!);
                        }
                        else
                        {
                            CollectFieldValues(property.Value, fieldName, values);
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        CollectFieldValues(item, fieldName, values);
                    }
                    break;
            }
        }
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync()
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<AnnotatedQuery>()
            .AddTypeExtension<AnnotatedQueryExtensions>()
            .AddTypeExtension<UserExtensions>()
            .AddTypeExtension<UserExtensionsWithFriend>()
            .AddTypeExtension<UserExtensionsWithPrefix>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static async Task<IExecutionResult> ExecuteAsync(
        IRequestExecutor executor,
        IOperationRequest request)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        return await Task
            .Run(() => executor.ExecuteAsync(request, cancellationToken), cancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
    }

    public interface IUser
    {
        string Name { get; }
    }

    public record User(int Id, string Name) : IUser;

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

    [ExtendObjectType<AnnotatedQuery>]
    public class AnnotatedQueryExtensions
    {
        public string GetHello() => "world";

        [BatchResolver]
        public List<string> GetMotto([Parent] List<AnnotatedQuery> queries)
            => queries.Select(_ => "Keep it simple.").ToList();
    }

    [ExtendObjectType<User>]
    public class UserExtensionsWithFriend
    {
        public async Task<User> GetFriendAsync([Parent] User user)
        {
            await Task.Yield();
            return new User(user.Id + 100, $"Friend of {user.Name}");
        }
    }

    [ExtendObjectType<User>]
    public class UserExtensionsWithPrefix
    {
        [BatchResolver]
        public List<string> GetGreetingWithPrefix(
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

    public class UserNameByIdDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
        : BatchDataLoader<int, string>(batchScheduler, options)
    {
        private static readonly Dictionary<int, string> s_names = new()
        {
            [1] = "Alice",
            [2] = "Bob",
            [3] = "Charlie",
            [101] = "Friend of Alice",
            [102] = "Friend of Bob",
            [103] = "Friend of Charlie"
        };

        public List<IReadOnlyList<int>> Loads { get; } = [];

        protected override Task<IReadOnlyDictionary<int, string>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            Loads.Add(keys.Order().ToArray());

            return Task.FromResult<IReadOnlyDictionary<int, string>>(
                keys.ToDictionary(k => k, k => s_names[k]));
        }
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
