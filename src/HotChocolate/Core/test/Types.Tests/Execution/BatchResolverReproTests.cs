using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

// This class collects engine-level batch resolver repros that fail by design today.
// Each test asserts the behavior a default user expects (the post-fix behavior) so the
// red is intentional and documents a known product gap. Do not weaken the assertions.
public class BatchResolverReproTests
{
    [Fact]
    public async Task Use_FieldMiddleware_Should_Apply_When_FieldIsBatchResolved()
    {
        // act
        // REPRO: descriptor .Use(...) field middleware on a batch field is dead code.
        // The batch strategy never invokes the regular field pipeline, so the result is
        // never rewritten. A default user expects the middleware to wrap the value.
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<ReproUser>>>()
                        .Resolve(new List<ReproUser>
                        {
                            new(1, "Alice"),
                            new(2, "Bob")
                        });
                })
                .AddObjectType<ReproUser>(d =>
                {
                    d.Field(u => u.Name);
                    d.Field("greeting")
                        .Type<StringType>()
                        .Use(next => async ctx =>
                        {
                            await next(ctx);
                            ctx.Result = $"wrapped({ctx.Result})";
                        })
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var user = contexts[i].Parent<ReproUser>();
                                results[i] = ResolverResult.Ok($"Hello, {user.Name}!");
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
                    "greeting": "wrapped(Hello, Alice!)"
                  },
                  {
                    "greeting": "wrapped(Hello, Bob!)"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Encode_GlobalId_When_IdApplied()
    {
        // arrange
        // REPRO: result formatters (the [ID]/.ID() global-id encoder is registered as a
        // ResultFormatterConfiguration) are skipped on batch fields, so the raw internal
        // value leaks instead of the opaque global id.
        var product1 = Convert.ToBase64String("Product:1"u8);
        var product2 = Convert.ToBase64String("Product:2"u8);

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification(false)
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("products")
                        .Type<ListType<ObjectType<ReproProduct>>>()
                        .Resolve(new List<ReproProduct>
                        {
                            new(1, "Product 1"),
                            new(2, "Product 2")
                        });
                })
                .AddObjectType<ReproProduct>(d =>
                {
                    d.Field(p => p.Name);
                    d.Field("externalId")
                        .Type<IntType>()
                        .ID("Product")
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var product = contexts[i].Parent<ReproProduct>();
                                results[i] = ResolverResult.Ok(product.Id);
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .ExecuteRequestAsync(
                    """
                    {
                        products {
                            externalId
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            $$"""
            {
              "data": {
                "products": [
                  {
                    "externalId": "{{product1}}"
                  },
                  {
                    "externalId": "{{product2}}"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BatchResolver_Should_Run_OwnResolver_Per_Type_When_AbstractParentHasSameNamedFields(
        bool useAttributes)
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("heroes")
                        .Type<ListType<InterfaceType<ICharacter>>>()
                        .Resolve(new List<ICharacter>
                        {
                            new Human(1, "Luke"),
                            new Droid(2, "R2D2")
                        });
                })
                .AddInterfaceType<ICharacter>(d => d.Field(c => c.Name));

        if (useAttributes)
        {
            builder
                .AddObjectType<Human>(d => d.Implements<InterfaceType<ICharacter>>())
                .AddObjectType<Droid>(d => d.Implements<InterfaceType<ICharacter>>())
                .AddTypeExtension<HumanExtension>()
                .AddTypeExtension<DroidExtension>();
        }
        else
        {
            builder
                .AddObjectType<Human>(d =>
                {
                    d.Implements<InterfaceType<ICharacter>>();
                    d.Field(h => h.Name);
                    d.Field("friends")
                        .Type<ListType<StringType>>()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var human = contexts[i].Parent<Human>();
                                results[i] = ResolverResult.Ok(
                                    new List<string> { $"human-friend-of-{human.Name}" });
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .AddObjectType<Droid>(d =>
                {
                    d.Implements<InterfaceType<ICharacter>>();
                    d.Field(dr => dr.Name);
                    d.Field("friends")
                        .Type<ListType<StringType>>()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var droid = contexts[i].Parent<Droid>();
                                results[i] = ResolverResult.Ok(
                                    new List<string> { $"droid-friend-of-{droid.Name}" });
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                });
        }

        // act
        var result =
            await builder
                .ExecuteRequestAsync(
                    """
                    {
                        heroes {
                            name
                            ... on Human { friends }
                            ... on Droid { friends }
                        }
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "heroes": [
                  {
                    "name": "Luke",
                    "friends": [
                      "human-friend-of-Luke"
                    ]
                  },
                  {
                    "name": "R2D2",
                    "friends": [
                      "droid-friend-of-R2D2"
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Preserve_Serial_Order_When_MutationRootField()
    {
        // arrange
        // REPRO: a batch resolver on a mutation root field bypasses serial mutation
        // execution. InferStrategy gives Batch precedence over Serial, so the batch field
        // dispatches before its serial siblings regardless of document order.
        MutationLog.Entries.Clear();

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Name("Query").Field("noop").Resolve("noop"))
                .AddMutationType(d =>
                {
                    d.Name("Mutation");
                    d.Field("appendLog")
                        .Argument("s", a => a.Type<NonNullType<StringType>>())
                        .Type<StringType>()
                        .Resolve(ctx =>
                        {
                            var s = ctx.ArgumentValue<string>("s");
                            MutationLog.Entries.Add(s);
                            return s;
                        });
                    d.Field("batchAppendLog")
                        .Argument("s", a => a.Type<NonNullType<StringType>>())
                        .Type<StringType>()
                        .ResolveBatch(contexts =>
                        {
                            var results = new ResolverResult[contexts.Count];

                            for (var i = 0; i < contexts.Count; i++)
                            {
                                var s = contexts[i].ArgumentValue<string>("s");
                                MutationLog.Entries.Add(s);
                                results[i] = ResolverResult.Ok(s);
                            }

                            return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                        });
                })
                .ExecuteRequestAsync(
                    """
                    mutation {
                        a: appendLog(s: "1")
                        b: batchAppendLog(s: "2")
                        c: appendLog(s: "3")
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        // Per the GraphQL spec top-level mutation fields execute serially in document
        // order, so the log must be [1, 2, 3]. Today the batch field runs first ([2, 1, 3]).
        Assert.Equal(["1", "2", "3"], MutationLog.Entries);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": "1",
                "b": "2",
                "c": "3"
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_Build_And_Execute_When_AttributeReturnsTaskOfList()
    {
        // act
        // REPRO: the attribute path infers the field type via GetListElementType(returnType)
        // which does not unwrap Task<>/ValueTask<>, so an async [BatchResolver] returning
        // Task<List<T>> throws BatchResolver_ReturnTypeMustBeList at schema build, even though
        // the batch compiler supports async signatures.
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<ListType<ObjectType<ReproUser>>>()
                        .Resolve(new List<ReproUser>
                        {
                            new(1, "Alice"),
                            new(2, "Bob")
                        });
                })
                .AddObjectType<ReproUser>(d => d.Field(u => u.Name))
                .AddTypeExtension<AsyncUserExtension>()
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
                  }
                ]
              }
            }
            """);
    }

    public interface ICharacter
    {
        string Name { get; }
    }

    public record Human(int Id, string Name) : ICharacter;

    public record Droid(int Id, string Name) : ICharacter;

    [ExtendObjectType<Human>]
    public class HumanExtension
    {
        [BatchResolver]
        public List<List<string>> GetFriends([Parent] List<Human> parents)
            => parents.Select(p => new List<string> { $"human-friend-of-{p.Name}" }).ToList();
    }

    [ExtendObjectType<Droid>]
    public class DroidExtension
    {
        [BatchResolver]
        public List<List<string>> GetFriends([Parent] List<Droid> parents)
            => parents.Select(p => new List<string> { $"droid-friend-of-{p.Name}" }).ToList();
    }

    public record ReproUser(int Id, string Name);

    public record ReproProduct(int Id, string Name);

    public static class MutationLog
    {
        public static List<string> Entries { get; } = [];
    }

    [ExtendObjectType<ReproUser>]
    public class AsyncUserExtension
    {
        [BatchResolver]
        public async Task<List<string>> GetGreeting([Parent] List<ReproUser> users)
        {
            await Task.Yield();

            var result = new List<string>();

            foreach (var user in users)
            {
                result.Add($"Hello, {user.Name}!");
            }

            return result;
        }
    }
}
