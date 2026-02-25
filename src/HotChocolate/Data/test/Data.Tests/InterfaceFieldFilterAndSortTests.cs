using HotChocolate.Types;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class InterfaceFieldFilterAndSortTests
{
    [Fact]
    public async Task Interface_Field_Exposes_Filter_And_Sort_Arguments()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddQueryType<Query>()
            .AddType<Author>()
            .BuildRequestExecutorAsync();

        var personType = executor.Schema.Types.GetType<InterfaceType>("Person");
        var friends = personType.Fields["friends"];

        Assert.Contains(friends.Arguments, argument => argument.Name == "where");
        Assert.Contains(friends.Arguments, argument => argument.Name == "order");
    }

    [Fact]
    public async Task Interface_Field_Arguments_Match_Object_Type()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddQueryType<Query>()
            .AddType<Author>()
            .BuildRequestExecutorAsync();

        var interfaceType = executor.Schema.Types.GetType<InterfaceType>("Person");
        var objectType = executor.Schema.Types.GetType<ObjectType>("Author");

        var interfaceField = interfaceType.Fields["friends"];
        var objectField = objectType.Fields["friends"];

        var interfaceWhere = interfaceField.Arguments["where"];
        var objectWhere = objectField.Arguments["where"];
        Assert.Equal(objectWhere.Type.Print(), interfaceWhere.Type.Print());

        var interfaceOrder = interfaceField.Arguments["order"];
        var objectOrder = objectField.Arguments["order"];
        Assert.Equal(objectOrder.Type.Print(), interfaceOrder.Type.Print());
    }

    [Fact]
    public async Task Interface_Field_Schema_Snapshot()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddQueryType<Query>()
            .AddType<Author>()
            .BuildRequestExecutorAsync();

        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Interface_Field_Filter_Execution()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddQueryType<QueryWithData>()
            .AddType<AuthorWithData>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                person {
                    friends(where: { name: { eq: "Alice" } }) {
                        nodes {
                            name
                        }
                    }
                }
            }
            """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Interface_Field_Sort_Execution()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddQueryType<QueryWithData>()
            .AddType<AuthorWithData>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                person {
                    friends(order: { name: DESC }) {
                        nodes {
                            name
                        }
                    }
                }
            }
            """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Interface_Field_Filter_And_Sort_Execution()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddQueryType<QueryWithData>()
            .AddType<AuthorWithData>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                person {
                    friends(
                        where: { name: { neq: "Charlie" } }
                        order: { name: ASC }
                    ) {
                        nodes {
                            name
                        }
                    }
                }
            }
            """);

        result.MatchSnapshot();
    }

    public class Query
    {
        public Person GetPerson() => new Author { Name = "Author" };
    }

    public class QueryWithData
    {
        public PersonWithData GetPerson() => new AuthorWithData { Name = "Author" };
    }

    [InterfaceType]
    public abstract class Person
    {
        public string Name { get; set; } = default!;

        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public IEnumerable<Person> Friends()
            => [new Author { Name = "Friend" }];
    }

    public sealed class Author : Person;

    [InterfaceType]
    public abstract class PersonWithData
    {
        public string Name { get; set; } = default!;

        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public virtual IEnumerable<PersonWithData> Friends()
            =>
            [
                new AuthorWithData { Name = "Charlie" },
                new AuthorWithData { Name = "Alice" },
                new AuthorWithData { Name = "Bob" }
            ];
    }

    public sealed class AuthorWithData : PersonWithData;
}
