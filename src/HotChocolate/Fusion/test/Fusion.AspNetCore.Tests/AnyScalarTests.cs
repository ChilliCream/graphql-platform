using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class AnyScalarTests : FusionTestBase
{
    [Fact]
    public async Task Handle_Any_Scalar_Object()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>().AddJsonTypeConverter());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              object
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Handle_Any_Scalar_SimpleValues()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>().AddJsonTypeConverter());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              simpleValues
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Handle_Any_Scalar_List()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>().AddJsonTypeConverter());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              list
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Handle_Any_Scalar_NestedObject()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>().AddJsonTypeConverter());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              nestedObject
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Handle_Any_Scalar_ListOfObjects()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>().AddJsonTypeConverter());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              listOfObjects
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Handle_Any_Scalar_ObjectWithLists()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>().AddJsonTypeConverter());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              objectWithLists
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Handle_Any_Scalar_ComplexNested()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>().AddJsonTypeConverter());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              complexNested
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Handle_Any_Scalar_NullValue()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>().AddJsonTypeConverter());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              nullValue
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    public class Query
    {
        [GraphQLType<AnyType>]
        public Dictionary<string, object?> GetObject()
            => new() { { "abc", "def" } };

        [GraphQLType<AnyType>]
        public Dictionary<string, object?> GetSimpleValues()
            => new()
            {
                { "string", "hello" },
                { "int", 42 },
                { "double", 3.14 },
                { "bool", true },
                { "null", null }
            };

        [GraphQLType<AnyType>]
        public List<object?> GetList()
            => ["string", 123, true, 45.67, null];

        [GraphQLType<AnyType>]
        public Dictionary<string, object?> GetNestedObject()
            => new()
            {
                { "name", "test" },
                {
                    "address", new Dictionary<string, object?>
                    {
                        { "street", "Main St" },
                        { "number", 123 },
                        { "city", "Springfield" }
                    }
                }
            };

        [GraphQLType<AnyType>]
        public List<object?> GetListOfObjects()
            =>
            [
                new Dictionary<string, object?> { { "id", 1 }, { "name", "Alice" } },
                new Dictionary<string, object?> { { "id", 2 }, { "name", "Bob" } },
                new Dictionary<string, object?> { { "id", 3 }, { "name", "Charlie" } }
            ];

        [GraphQLType<AnyType>]
        public Dictionary<string, object?> GetObjectWithLists()
            => new()
            {
                { "name", "Product" },
                { "tags", new List<object?> { "electronics", "gadget", "popular" } },
                { "prices", new List<object?> { 99.99, 89.99, 79.99 } }
            };

        [GraphQLType<AnyType>]
        public Dictionary<string, object?> GetComplexNested()
            => new()
            {
                { "id", 1 },
                { "name", "Root" },
                {
                    "children", new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            { "id", 2 },
                            { "name", "Child1" },
                            { "values", new List<object?> { 1, 2, 3 } }
                        },
                        new Dictionary<string, object?>
                        {
                            { "id", 3 },
                            { "name", "Child2" },
                            {
                                "metadata", new Dictionary<string, object?>
                                {
                                    { "created", "2024-01-01" },
                                    { "tags", new List<object?> { "tag1", "tag2" } }
                                }
                            }
                        }
                    }
                }
            };

        [GraphQLType<AnyType>]
        public object? GetNullValue() => null;
    }
}
