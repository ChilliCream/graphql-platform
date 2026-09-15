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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BatchResolver_Should_NotInvoke_When_ParentCollectionContainsOnlyNulls(bool attributes)
    {
        // arrange
        var probe = new NullParentProbe();
        var builder = new ServiceCollection().AddSingleton(probe).AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("nullCollection")
                    .Type<ListType<ObjectType<ReproUser>>>()
                    .Resolve(_ => (object?)null);
                d.Field("allNullParents")
                    .Type<ListType<ObjectType<ReproUser>>>()
                    .Resolve(new ReproUser?[] { null, null });
                d.Field("users")
                    .Resolve(new[] { new ReproUser(1, "Alice"), new ReproUser(2, "Bob") });
            });

        if (attributes)
        {
            builder.AddTypeExtension<NullParentUserExtension>();
        }
        else
        {
            builder.AddObjectType<ReproUser>(d => d.Field("value")
                .ResolveBatchWith<NullParentUserExtension>(t => t.GetValue(default!, default!)));
        }

        var executor = await builder.BuildRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var nullResult = await executor.ExecuteAsync(
            "{ nullCollection { value } allNullParents { value } }",
            cancellationToken: TestContext.Current.CancellationToken);
        var nullInvocations = probe.Invocations;
        await using var controlResult = await executor.ExecuteAsync(
            "{ users { value } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot(postFix: attributes.ToString())
            .Add(nullResult, "Null parents")
            .Add(nullInvocations, "Invocations for null parents")
            .Add(controlResult, "Non-null control")
            .Add(probe.Invocations, "Invocations after control")
            .MatchMarkdownSnapshot();
        Assert.Equal(0, nullInvocations);
        Assert.Equal(1, probe.Invocations);
    }

    public sealed class NullParentProbe
    {
        public int Invocations { get; set; }
    }

    [ExtendObjectType<ReproUser>]
    public class NullParentUserExtension
    {
        [BatchResolver]
        public List<string> GetValue([Parent] List<ReproUser> users, [Service] NullParentProbe probe)
        {
            probe.Invocations++;
            return users.Select(u => u.Name).ToList();
        }
    }

    public static TheoryData<bool, string, string> DistributionCases()
    {
        var cases = new TheoryData<bool, string, string>();

        foreach (var attributes in new[] { false, true })
        {
            foreach (var mode in new[] { "null", "empty", "short", "long", "exact", "nullElement", "allNull", "nonList", "indexer", "count" })
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

    [Fact]
    public async Task BatchResolver_Should_RejectHashSetParent_When_BuildingSchema()
    {
        // arrange
        // a [Parent] parameter with a non-list shape (here HashSet<T>) must raise the same
        // schema error text on the reflection path as the source-generated path, naming the
        // member and parameter.
        var builder = new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("users").Resolve(new[] { new ReproUser(1, "A") }))
            .AddObjectType<ReproUser>(d => d.Field("value").ResolveBatchWith(
                typeof(HashSetParentUserExtension).GetMethod(nameof(HashSetParentUserExtension.GetValue))!));

        // act
        var error = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        error.Errors.Select(e => e.Message).ToArray().MatchInlineSnapshot(
            """
            [
              "The parameter 'HotChocolate.Execution.BatchResolverReproTests+HashSetParentUserExtension.GetValue(users)' on a batch resolver must be a list type (e.g. List<T>, IReadOnlyList<T>, ImmutableArray<T> or T[]). Batch resolvers receive one value per parent object, so all argument parameters must be collections."
            ]
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_RejectHashSetArgument_When_BuildingSchema()
    {
        // arrange
        // an argument parameter with a non-list shape (here HashSet<T>) must raise the same
        // schema error text on the reflection path as the source-generated path, naming the
        // member and parameter.
        var builder = new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("users").Resolve(new[] { new ReproUser(1, "A") }))
            .AddTypeExtension<HashSetArgumentUserExtension>();

        // act
        var error = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        error.Errors.Select(e => e.Message).ToArray().MatchInlineSnapshot(
            """
            [
              "For more details look at the `Errors` property.\n\n1. The parameter 'HotChocolate.Execution.BatchResolverReproTests+HashSetArgumentUserExtension.GetValue(prefix)' on a batch resolver must be a list type (e.g. List<T>, IReadOnlyList<T>, ImmutableArray<T> or T[]). Batch resolvers receive one value per parent object, so all argument parameters must be collections.\n"
            ]
            """);
    }

    // Registered explicitly with ResolveBatchWith, not [ExtendObjectType<ReproUser>], so the
    // field reaches BatchResolverCompiler and this schema error, rather than attribute discovery.
    public class HashSetParentUserExtension
    {
        [BatchResolver]
        public List<string> GetValue([Parent] HashSet<ReproUser> users)
            => users.Select(u => u.Name).ToList();
    }

    [ExtendObjectType<ReproUser>]
    public class HashSetArgumentUserExtension
    {
        [BatchResolver]
        public List<string> GetValue([Parent] List<ReproUser> users, HashSet<string> prefix)
            => users.Select(u => u.Name).ToList();
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
                "allNull" => [null, null],
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

    public record ReproUser(int Id, string Name);
}
