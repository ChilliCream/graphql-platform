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
