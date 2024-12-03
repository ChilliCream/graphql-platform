using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;

namespace HotChocolate;

public class SchemaBuilderExtensionsResolversTests
{
    [Fact]
    public void AddResolverContextObject_BuilderIsNull_ArgNullExcept()
    {
        // arrange
        var builder = new SchemaBuilder();

        // act
        Action action = () => SchemaBuilderExtensions
            .AddResolver(
                null,
                "A",
                "B",
                new Func<IResolverContext, object>(c => new object()));

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AddResolverContextObject_ResolverIsNull_ArgNullExcept()
    {
        // arrange
        var builder = new SchemaBuilder();

        // act
        Action action = () => SchemaBuilderExtensions
            .AddResolver(
                builder,
                "A",
                "B",
                (Func<IResolverContext, object>)null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task AddResolverContextObject_ResolveField()
    {
        // arrange
        var builder = new SchemaBuilder();
        builder.AddDocumentFromString("type Query { foo: String }");

        // act
        builder.AddResolver(
            "Query",
            "foo",
            new Func<IResolverContext, object>(_ => "bar"));

        // assert
        await builder.Create()
            .MakeExecutable()
            .ExecuteAsync("{ foo }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public void AddResolverContextTaskObject_BuilderIsNull_ArgNullExcept()
    {
        // arrange
        var builder = new SchemaBuilder();

        // act
        Action action = () => SchemaBuilderExtensions
            .AddResolver(
                null,
                "A",
                "B",
                new Func<IResolverContext, Task<object>>(
                    c => Task.FromResult(new object())));

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AddResolverContextTaskObject_ResolverIsNull_ArgNullExcept()
    {
        // arrange
        var builder = new SchemaBuilder();

        // act
        Action action = () => SchemaBuilderExtensions
            .AddResolver(
                builder,
                "A",
                "B",
                (Func<IResolverContext, Task<object>>)null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task AddResolverContextTaskObject_ResolveField()
    {
        // arrange
        var builder = new SchemaBuilder();
        builder.AddDocumentFromString("type Query { foo: String }");

        // act
        SchemaBuilderExtensions
            .AddResolver(
                builder,
                "Query",
                "foo",
                new Func<IResolverContext, ValueTask<object>>(
                    c => new ValueTask<object>("bar")));

        // assert
        await builder.Create()
            .MakeExecutable()
            .ExecuteAsync("{ foo }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public void AddResolverContextTResult_BuilderIsNull_ArgNullExcept()
    {
        // arrange
        var builder = new SchemaBuilder();

        // act
        Action action = () => SchemaBuilderExtensions
            .AddResolver(
                null,
                "A",
                "B",
                new Func<IResolverContext, string>(
                    c => "abc"));

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AddResolverContextTResult_ResolverIsNull_ArgNullExcept()
    {
        // arrange
        var builder = new SchemaBuilder();

        // act
        Action action = () => SchemaBuilderExtensions
            .AddResolver(
                builder,
                "A",
                "B",
                (Func<IResolverContext, string>)null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task AddResolverContextTResult_ResolveField()
    {
        // arrange
        var builder = new SchemaBuilder();
        builder.AddDocumentFromString("type Query { foo: String }");

        // act
        SchemaBuilderExtensions
            .AddResolver(
                builder,
                "Query",
                "foo",
                new Func<IResolverContext, string>(
                    c => "bar"));

        // assert
        await builder.Create()
            .MakeExecutable()
            .ExecuteAsync("{ foo }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public void AddResolverContextTaskTResult_BuilderIsNull_ArgNullExcept()
    {
        // arrange
        var builder = new SchemaBuilder();

        // act
        Action action = () => SchemaBuilderExtensions
            .AddResolver(
                null,
                "A",
                "B",
                new Func<IResolverContext, Task<string>>(
                    c => Task.FromResult("abc")));

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AddResolverContextTaskTResult_ResolverIsNull_ArgNullExcept()
    {
        // arrange
        var builder = new SchemaBuilder();

        // act
        Action action = () => SchemaBuilderExtensions
            .AddResolver(
                builder,
                "A",
                "B",
                (Func<IResolverContext, Task<string>>)null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async  Task AddResolverContextTaskTResult_ResolveField()
    {
        // arrange
        var builder = new SchemaBuilder();
        builder.AddDocumentFromString("type Query { foo: String }");

        // act
        SchemaBuilderExtensions
            .AddResolver(
                builder,
                "Query",
                "foo",
                new Func<IResolverContext, ValueTask<string>>(
                    c => new ValueTask<string>("bar")));

        // assert
        await builder.Create()
            .MakeExecutable()
            .ExecuteAsync("{ foo }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public void AddResolverObject_BuilderIsNull_ArgNullExcept()
    {
        // arrange
        var builder = new SchemaBuilder();

        // act
        Action action = () => SchemaBuilderExtensions
            .AddResolver(
                null,
                "A",
                "B",
                new Func<object>(() => "abc"));

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AddResolverObject_ResolverIsNull_ArgNullExcept()
    {
        // arrange
        var builder = new SchemaBuilder();

        // act
        Action action = () => SchemaBuilderExtensions
            .AddResolver(
                builder,
                "A",
                "B",
                (Func<object>)null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task AddResolverObject_ResolveField()
    {
        // arrange
        var builder = new SchemaBuilder();
        builder.AddDocumentFromString("type Query { foo: String }");

        // act
        SchemaBuilderExtensions
            .AddResolver(
                builder,
                "Query",
                "foo",
                new Func<object>(() => "bar"));

        // assert
        await builder.Create()
            .MakeExecutable()
            .ExecuteAsync("{ foo }")
            .MatchSnapshotAsync();
    }
}
