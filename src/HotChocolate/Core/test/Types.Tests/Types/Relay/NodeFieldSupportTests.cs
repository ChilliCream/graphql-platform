using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Relay;

public class NodeFieldSupportTests
{
    [Fact]
    public async Task Node_Resolve_Separated_Resolver()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo>()
                .AddObjectType<Bar>(d => d
                    .ImplementsNode()
                    .IdField(t => t.Id)
                    .ResolveNodeWith<BarResolver>(t => t.GetBarAsync(default)))
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Nodes_Get_Single()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo>()
                .AddObjectType<Bar>(d => d
                    .ImplementsNode()
                    .IdField(t => t.Id)
                    .ResolveNodeWith<BarResolver>(t => t.GetBarAsync(default)))
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ nodes(ids: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Tow_Many_Nodes()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo>()
                .AddObjectType<Bar>(d => d
                    .ImplementsNode()
                    .IdField(t => t.Id)
                    .ResolveNodeWith<BarResolver>(t => t.GetBarAsync(default)))
                .ModifyOptions(o => o.MaxAllowedNodeBatchSize = 1)
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ nodes(ids: [\"QmFyOjEyMw==\", \"QmFyOjEyMw==\"]) { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Nodes_Get_Many()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo>()
                .AddObjectType<Bar>(d => d
                    .ImplementsNode()
                    .IdField(t => t.Id)
                    .ResolveNodeWith<BarResolver>(t => t.GetBarAsync(default)))
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ nodes(ids: [\"QmFyOjEyMw==\", \"QmFyOjEyMw==\"]) { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Parent_Id()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType(
                    x => x.Name("Query")
                        .Field("childs")
                        .Resolve(new Child { Id = "123", }))
                .AddObjectType<Child>(d => d
                    .ImplementsNode()
                    .IdField(t => t.Id)
                    .ResolveNode((_, id) => Task.FromResult(new Child { Id = id, })))
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ childs { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Separated_Resolver_ImplicitId()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo>()
                .AddObjectType<Bar>(d => d
                    .ImplementsNode()
                    .ResolveNodeWith<BarResolver>(t => t.GetBarAsync(default)))
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Implicit()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo1>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Implicit_Resolver()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Bar5>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Implicit_Named_Resolver()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo2>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Implicit_Inherited_Resolver()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo6>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Implicit_External_Resolver()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo3>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Implicit_ExternalInheritedStatic_Resolver()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo7>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Implicit_ExternalInheritedInstance_Resolver()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo8>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Implicit_ExternalDefinedOnInterface_Resolver()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddSingleton<IBar9Resolver, Bar9Resolver>()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType<Foo9>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolve_Implicit_Custom_IdField()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddQueryType<Foo4>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"QmFyOjEyMw==\") { id } }");

        // assert
        result.MatchSnapshot();
    }

    public class Foo
    {
        public Bar Bar { get; set; } = new() { Id = "123", };
    }

    public class Bar
    {
        public string Id { get; set; }
    }

    public class BarResolver
    {
        public Task<Bar> GetBarAsync(string id) => Task.FromResult(new Bar { Id = id, });
    }

    public class Foo1
    {
        public Bar1 Bar { get; set; } = new() { Id = "123", };
    }

    [ObjectType("Bar")]
    [Node]
    public class Bar1
    {
        public string Id { get; set; }

        public static Bar1 GetBar1(string id) => new() { Id = id, };
    }

    public class Foo2
    {
        public Bar2 Bar { get; set; } = new() { Id = "123", };
    }

    [ObjectType("Bar")]
    [Node(NodeResolver = nameof(GetFoo))]
    public class Bar2
    {
        public string Id { get; set; }

        public static Bar2 GetFoo(string id) => new() { Id = id, };
    }

    public class Foo3
    {
        public Bar3 Bar { get; set; } = new() { Id = "123", };
    }

    [ObjectType("Bar")]
    [Node(NodeResolverType = typeof(Bar3Resolver))]
    public class Bar3
    {
        public string Id { get; set; }
    }

    public static class Bar3Resolver
    {
        public static Bar3 GetBar3(string id) => new() { Id = id, };
    }

    public class Foo4
    {
        public Bar4 Bar { get; set; } = new() { Id1 = "123", };
    }

    [ObjectType("Bar")]
    [Node(
        IdField = nameof(Id1),
        NodeResolver = nameof(GetFoo))]
    public class Bar4
    {
        public string Id1 { get; set; }

        public static Bar2 GetFoo(string id) => new() { Id = id, };
    }

    [ObjectType("Bar")]
    [Node]
    public class Bar5
    {
        public string Id { get; set; }

        public static Bar5 Get(string id) => new() { Id = id, };
    }

    public class Foo6
    {
        public Bar6 Bar { get; set; } = new() { Id = "123", };
    }

    public abstract class Bar6Base<T> where T : Bar6Base<T>, new()
    {
        public string Id { get; set; }

        public static T Get(string id) => new() { Id = id, };
    }

    [ObjectType("Bar")]
    [Node]
    public class Bar6 : Bar6Base<Bar6>
    {
    }

    public class Foo7
    {
        public Bar7 Bar { get; set; } = new() { Id = "123", };
    }

    [ObjectType("Bar")]
    [Node(NodeResolverType = typeof(Bar7Resolver))]
    public class Bar7
    {
        public string Id { get; set; }
    }

    public abstract class Bar7ResolverBase
    {
        public static Bar7 GetBar7(string id) => new() { Id = id, };
    }

    public class Bar7Resolver : Bar7ResolverBase
    {
    }

    public class Foo8
    {
        public Bar8 Bar { get; set; } = new() { Id = "123", };
    }

    [ObjectType("Bar")]
    [Node(NodeResolverType = typeof(Bar8Resolver))]
    public class Bar8
    {
        public string Id { get; set; }
    }

    public class Bar8ResolverBase
    {
        public Bar8 GetBar8(string id) => new() { Id = id, };
    }

    public class Bar8Resolver : Bar8ResolverBase
    {
    }

    public class Foo9
    {
        public Bar9 Bar { get; set; } = new() { Id = "123", };
    }

    [ObjectType("Bar")]
    [Node(NodeResolverType = typeof(IBar9Resolver))]
    public class Bar9
    {
        public string Id { get; set; }
    }

    public interface IBar9Resolver
    {
        public Bar9 GetBar9(string id);
    }

    public class Bar9Resolver : IBar9Resolver
    {
        public Bar9 GetBar9(string id) => new() { Id = id, };
    }

    public class Parent
    {
        public string Id { get; set; }
    }

    public class Child : Parent
    {
    }
}
