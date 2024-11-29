using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class IntegrationTests
{
    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsFieldRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        var result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    baz
                }
            }
            ");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsListRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        var result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    qux
                }
            }
            ");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsObjectListRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        var result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    nestedList {
                        bar
                    }
                }
            }
            ");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsObjectRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        var result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    nested {
                        bar
                    }
                }
            }
            ");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolver_With_SingleOrDefault_Schema()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolver_With_SingleOrDefault()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              node(id: "Rm9vOkE=") {
                id
                __typename
                ... on Baz { fieldOfBaz }
                ... on Foo { fieldOfFoo }
                ... on Bar { fieldOfBar }
              }
            }
            """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolver_With_SingleOrDefault_Fragments()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync();

        var result = await executor
            .ExecuteAsync("""
                {
                    node(id: "Rm9vOkE=") {
                        id
                        __typename
                        ... on Baz { fieldOfBaz }
                        ... on Foo { fieldOfFoo }
                    }
                }
                """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Node_Resolver_Without_SingleOrDefault()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              node(id: "QmFyOkE=") {
                id
                __typename
                ... on Baz { fieldOfBaz }
                ... on Foo { fieldOfFoo }
                ... on Bar { fieldOfBar }
              }
            }
            """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Nodes_Resolver_With_SingleOrDefault()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(@"{ nodes(ids: ""Rm9vOkE="") { id __typename } }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Nodes_Resolver_With_SingleOrDefault_Fragments()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync();

        var result = await executor
            .ExecuteAsync("""
                {
                    nodes(ids: "Rm9vOkE=") {
                        id
                        __typename
                        ... on Baz { fieldOfBaz }
                        ... on Foo { fieldOfFoo }
                    }
                }
                """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Nodes_Resolver_Without_SingleOrDefault()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddProjections()
            .BuildRequestExecutorAsync();

        var result = await executor
            .ExecuteAsync("""
                {
                    nodes(ids: "QmFyOkE=") {
                        id
                        __typename
                        ... on Baz { fieldOfBaz }
                        ... on Foo { fieldOfFoo }
                        ... on Bar { fieldOfBar }
                    }
                }
                """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_Select()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>() //error thrown without query, it's not needed for the test though
            .AddMutationType<Mutation>()
            .AddProjections()
            .AddMutationConventions()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
             """
              mutation {
                  modify {
                      foo {
                          bar
                      }
                  }
              }
              """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_HasError()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>() //error thrown without query, it's not needed for the test though
            .AddMutationType<Mutation>()
            .AddProjections()
            .AddMutationConventions()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            mutation {
                createRecord(input: {throwError: false}) {
                    foo {
                        bar
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                }
            }
            """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_ThrowsError()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>() //error thrown without query, it's not needed for the test though
            .AddMutationType<Mutation>()
            .AddProjections()
            .AddMutationConventions()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            mutation {
                createRecord(input: {throwError: true}) {
                    foo {
                        bar
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                }
            }
            """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_Select_With_SingleOrDefault()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>() //error thrown without query, it's not needed for the test though
            .AddMutationType<Mutation>()
            .AddProjections()
            .AddMutationConventions()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
             """
              mutation {
                  modifySingleOrDefault {
                      foo {
                          bar
                      }
                  }
              }
              """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_With_Relay_Projection_Schema()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddMutationType<Mutation>()
            .AddQueryFieldToMutationPayloads()
            .AddProjections()
            .AddMutationConventions()
            .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Convention_With_Relay_Projection()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithNodeResolvers>()
            .AddObjectType<Foo>(d => d.ImplementsNode().IdField(t => t.Bar))
            .AddObjectType<Bar>(d => d.ImplementsNode().IdField(t => t.IdOfBar))
            .AddObjectType<Baz>(d => d.ImplementsNode().IdField(t => t.Bar2))
            .AddGlobalObjectIdentification()
            .AddMutationType<Mutation>()
            .AddQueryFieldToMutationPayloads()
            .AddProjections()
            .AddMutationConventions()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            mutation {
                createRecord(input: {throwError: false}) {
                    foo {
                        id
                        fieldOfFoo
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                    query {
                        node(id: "QmFyOkE=") {
                            id
                            __typename
                            ... on Baz { fieldOfBaz }
                            ... on Foo { fieldOfFoo }
                            ... on Bar { fieldOfBar }
                        }
                    }
                }
            }
            """);

        result.MatchSnapshot();
    }
}

public class Query
{
    [UseProjection]
    public IQueryable<Foo> Foos
        => new Foo[] { new() { Bar = "A", }, new() { Bar = "B", }, }.AsQueryable();
}

public class Mutation
{
    [UseMutationConvention]
    [UseProjection]
    public IQueryable<Foo> Modify()
    {
        return new Foo[] { new() { Bar = "A", }, new() { Bar = "B", }, }.AsQueryable();
    }

    [UseMutationConvention]
    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Foo> ModifySingleOrDefault()
        => new Foo[] { new() { Bar = "A", }, }.AsQueryable();

    [Error<AnError>]
    [UseMutationConvention]
    [UseProjection]
    public IQueryable<Foo> CreateRecord(bool throwError)
    {
        if (throwError)
        {
            throw new AnError("this is only a test");
        }

        return new Foo[] { new() { Bar = "A", }, new() { Bar = "B", }, }.AsQueryable();
    }

    public class AnError : Exception
    {
        public AnError(string message) : base(message)
        {
        }
    }
}

[ExtendObjectType(typeof(Foo))]
public class FooExtensions
{
    public string Baz => "baz";

    public IEnumerable<string> Qux => new[] { "baz", };

    public IEnumerable<Foo> NestedList => new[] { new Foo() { Bar = "C", }, };

    public Foo Nested => new() { Bar = "C", };
}

public class Foo
{
    public string? Bar { get; set; }
    public string FieldOfFoo => "fieldOfFoo";
}

public class Baz
{
    public string? Bar2 { get; set; }

    public string FieldOfBaz => "fieldOfBaz";
}

public class Bar
{
    public string? IdOfBar { get; set; }

    public string FieldOfBar => "fieldOfBar";
}

public class QueryWithNodeResolvers
{
    [UseProjection]
    public IQueryable<Foo> All()
        => new Foo[] { new() { Bar = "A", }, }.AsQueryable();

    [NodeResolver]
    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Foo> GetById(string id)
        => new Foo[] { new() { Bar = "A", }, }.AsQueryable();

    [NodeResolver]
    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Baz> GetBazById(string id)
        => new Baz[] { new() { Bar2 = "A", }, }.AsQueryable();

    [NodeResolver]
    public Bar GetBarById(string id) => new() { IdOfBar = "A", };
}
