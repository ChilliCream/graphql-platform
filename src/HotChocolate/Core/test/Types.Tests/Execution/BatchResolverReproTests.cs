using System.Collections;
using System.Collections.Immutable;
using CookieCrumble;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

// Engine-level batch resolver regressions and pending contract repros.
public class BatchResolverReproTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 2)]
    public async Task BatchResolver_Should_BindShapes_When_ParentsAreSupported(bool attributes, int count)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("users")
                .Resolve(Enumerable.Range(1, count).Select(i => new ReproUser(i, $"User {i}")).ToList()));

        if (attributes)
        {
            builder.AddTypeExtension<ShapeUserExtension>();
        }
        else
        {
            builder.AddObjectType<ReproUser>(d =>
            {
                foreach (var name in new[] { "Array", "Immutable", "ReadOnly", "List", "Enumerable", "Interface" })
                {
                    d.Field($"{char.ToLowerInvariant(name[0])}{name[1..]}").ResolveBatchWith(
                        typeof(ShapeUserExtension).GetMethod($"Get{name}")!);
                }
            });
        }

        // act
        await using var result = await builder.ExecuteRequestAsync(
            "{ users { array immutable readOnly list enumerable interface } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot(postFix: $"{attributes}_{count}")
            .Add(result, "Result")
            .MatchMarkdownSnapshot();
    }

    public static TheoryData<bool, string, string> DistributionCases()
    {
        var cases = new TheoryData<bool, string, string>();

        foreach (var attributes in new[] { false, true })
        {
            foreach (var mode in new[] { "null", "empty", "short", "long", "exact", "nullElement", "nonList", "indexer", "count" })
            {
                foreach (var field in new[] { "value", "taskValue", "valueTaskValue" })
                {
                    cases.Add(attributes, mode, field);
                }
            }
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(DistributionCases))]
    public async Task BatchResolver_Should_ValidateDistribution_When_ResultIsInvalid(
        bool attributes,
        string mode,
        string field)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("users")
                .Resolve(new[] { new ReproUser(1, mode), new ReproUser(2, mode) }));

        if (attributes)
        {
            builder.AddTypeExtension<DistributionUserExtension>();
        }
        else
        {
            builder.AddObjectType<ReproUser>(d => d.Field(field).ResolveBatchWith(
                typeof(DistributionUserExtension).GetMethod($"Get{char.ToUpperInvariant(field[0])}{field[1..]}")!));
        }

        // act
        await using var result = await builder.ExecuteRequestAsync(
            $"{{ users {{ value: {field} }} }}",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var operation = Assert.IsType<OperationResult>(result);
        new Snapshot(postFix: $"{attributes}_{mode}")
            .Add(result, "Result")
            .Add(operation.Errors?.Select(e => e.Exception?.Message).ToArray(), "Failure")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BatchResolver_Should_RejectEnumerableReturn_When_BuildingSchema(bool attributes)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("users").Resolve(new[] { new ReproUser(1, "A") }));

        if (attributes)
        {
            builder.AddTypeExtension<NonListUserExtension>();
        }
        else
        {
            builder.AddObjectType<ReproUser>(d => d.Field("value").ResolveBatchWith(
                typeof(NonListUserExtension).GetMethod(nameof(NonListUserExtension.GetValue))!));
        }

        // act
        var error = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        error.Errors.Select(e => e.Message).ToArray().MatchInlineSnapshot(
            """
            [
              "For more details look at the `Errors` property.\n\n1. The batch resolver method 'HotChocolate.Execution.BatchResolverReproTests+NonListUserExtension.GetValue' must return a list type (e.g. List<T>, IReadOnlyList<T>, ImmutableArray<T> or T[]). Batch resolvers return one result per parent object, so the return type must be a collection.\n"
            ]
            """);
    }

    [ExtendObjectType<ReproUser>]
    public class ShapeUserExtension
    {
        [BatchResolver]
        public string[] GetArray([Parent] ReproUser[] users)
            => users.Select(u => u.Name).ToArray();

        [BatchResolver]
        public ImmutableArray<string> GetImmutable([Parent] ImmutableArray<ReproUser> users)
            => users.Select(u => u.Name).ToImmutableArray();

        [BatchResolver]
        public IReadOnlyList<string> GetReadOnly([Parent] IReadOnlyList<ReproUser> users)
            => users.Select(u => u.Name).ToList();

        [BatchResolver]
        public List<string> GetList([Parent] List<ReproUser> users)
            => users.Select(u => u.Name).ToList();

        [BatchResolver]
        public async Task<string[]> GetEnumerable([Parent] IEnumerable<ReproUser> users)
        {
            await Task.Yield();
            return users.Select(u => u.Name).ToArray();
        }

        [BatchResolver]
        public async ValueTask<ImmutableArray<string>> GetInterface([Parent] IList<ReproUser> users)
        {
            await Task.Yield();
            return users.Select(u => u.Name).ToImmutableArray();
        }
    }

    [ExtendObjectType<ReproUser>]
    public class DistributionUserExtension
    {
        [BatchResolver]
        public IReadOnlyList<string?>? GetValue([Parent] List<ReproUser> users)
            => users[0].Name switch
            {
                "null" => null,
                "empty" => [],
                "short" => ["one"],
                "long" => ["one", "two", "three"],
                "nullElement" => ["one", null],
                "nonList" => new NonListResult(),
                "indexer" => new FailingList { "one", "two" },
                "count" => new FailingCountList { "one", "two" },
                _ => users.Select(u => (string?)u.Name).ToList()
            };

        [BatchResolver]
        public async Task<IReadOnlyList<string?>?> GetTaskValue([Parent] ReproUser[] users)
        {
            await Task.Yield();
            return GetValue(users.ToList());
        }

        [BatchResolver]
        public async ValueTask<IReadOnlyList<string?>?> GetValueTaskValue([Parent] ImmutableArray<ReproUser> users)
        {
            await Task.Yield();
            return GetValue(users.ToList());
        }
    }

    private sealed class NonListResult : IReadOnlyList<string?>
    {
        public int Count => 2;
        public string? this[int index] => "value";
        public IEnumerator<string?> GetEnumerator() => Enumerable.Repeat<string?>("value", 2).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class FailingList : List<string?>, IList
    {
        object? IList.this[int index]
        {
            get => index == 1 ? throw new InvalidOperationException("List index failed.") : this[index];
            set => this[index] = (string?)value;
        }
    }

    private sealed class FailingCountList : List<string?>, ICollection
    {
        int ICollection.Count => throw new InvalidOperationException("List count failed.");
    }

    [ExtendObjectType<ReproUser>]
    public class NonListUserExtension
    {
        [BatchResolver]
        public Task<IEnumerable<string>> GetValue([Parent] List<ReproUser> users)
            => Task.FromResult(users.Select(u => u.Name));
    }

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
                    d.Field(p => p.Id).ID("Product");
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
        // arrange
        var builder = new ServiceCollection()
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
                .AddTypeExtension<AsyncUserExtension>();

        // act
        await using var result = await builder.ExecuteRequestAsync(
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
