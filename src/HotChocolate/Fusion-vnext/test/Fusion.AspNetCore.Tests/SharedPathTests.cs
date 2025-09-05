using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class SharedPathTests : FusionTestBase
{
    [Fact]
    public async Task Single_Shared_Root_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                viewer {
                    schema1
                    schema2
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact(Skip = "SelectionSetPartitioner incorrectly does not include ... @skip")]
    public async Task Single_Shared_Root_Field_With_Inline_Fragment_Without_TypeCondition()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            query($skip: Boolean!) {
                viewer {
                    ... @skip(if: $skip) {
                        schema1
                        schema2
                    }
                }
            }
            """,
            new Dictionary<string, object?> { ["skip"] = false },
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Single_Shared_Interface_Root_Field_With_Type_Refinement()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                interface {
                    ... on Review {
                        schema1
                        schema2
                    }
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Single_Shared_Union_Root_Field_With_Type_Refinement()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                union {
                    ... on Review {
                        schema1
                        schema2
                    }
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Single_Shared_Root_Field_With_Extra_Fields_On_Root()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                viewer {
                    schema1
                    schema2
                }
                schema1
                schema2
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Hierarchy_Of_Shared_Root_Fields()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                viewer {
                    settings {
                        schema1
                        schema2
                    }
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Hierarchy_Of_Shared_Root_Fields_With_Extra_Fields_On_Shared_Level()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        using var server3 = CreateSourceSchema(
            "C",
            b => b.AddQueryType<SourceSchema3.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                viewer {
                    schema1
                    settings {
                        schema1
                        schema2
                    }
                    schema2
                    schema3
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Shared_Parent_Field_Below_Type_With_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                productById(id: 1) {
                    shared {
                        schema2
                        schema1
                    }
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Shared_Parent_Field_Below_Type_With_Lookup_With_Extra_Fields_On_Shared_Level()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                productById(id: 1) {
                    schema1
                    shared {
                        schema2
                        schema1
                    }
                    schema2
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    // TODO: This should've used the product lookup
    public async Task Shared_Parent_Field_Below_Type_With_Lookup_With_Type_Refinement()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                unsharedInterface {
                    ... on Product {
                        shared {
                            schema1
                            schema2
                        }
                    }
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Hierarchy_Of_Shared_Parent_Fields_Below_Type_With_Lookup()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                productById(id: 1) {
                    shared {
                        shared2 {
                            schema2
                            schema1
                        }
                    }
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Hierarchy_Of_Shared_Parent_Fields_Below_Type_With_Lookup_With_Extra_Fields_On_Shared_Level()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                productById(id: 1) {
                    shared {
                        schema1
                        shared2 {
                            schema2
                            schema1
                        }
                        schema2
                    }
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Hierarchy_Of_Shared_Parent_Fields_Below_Type_With_Lookup_With_Extra_Fields_On_Shared_Levels()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                productById(id: 1) {
                    schema1
                    shared {
                        schema1
                        shared2 {
                            schema2
                            schema1
                        }
                        schema2
                    }
                    schema2
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    public static class SourceSchema1
    {
        public class Query
        {
            public Viewer Viewer => new Viewer();

            public string Schema1 => "schema1";

            public IInterface Interface => new Review(1);

            public IInterface UnsharedInterface => new Product(2);

            public IUnion Union => new Review(2);

            public Review TopReview => new Review(3);

            [Lookup]
            public Product? GetProductById(int id) => new Product(id);
        }

        public class Viewer
        {
            public string Schema1 => "schema1";

            public ViewerSettings Settings => new ViewerSettings();
        }

        public class ViewerSettings
        {
            public string Schema1 => "schema1";
        }

        public record Product(int Id) : IInterface, IUnion
        {
            public string Schema1 => "schema1";

            public SharedProduct? Shared => new SharedProduct();
        }

        public record Review(int Id) : IInterface, IUnion
        {
            public string Schema1 => "schema1";
        }

        public class SharedProduct
        {
            public string Schema1 => "schema1";

            public SharedProduct2 ? Shared2 => new SharedProduct2();
        }

        public class SharedProduct2
        {
            public string Schema1 => "schema1";
        }

        public interface IInterface
        {
            int Id { get; }
        }

        [UnionType]
        public interface IUnion;
    }

    public static class SourceSchema2
    {
        public class Query
        {
            public Viewer Viewer => new Viewer();

            public string Schema2 => "schema2";

            public IInterface Interface => new Review(1);

            public IUnion Union => new Review(2);

            public Review TopReview => new Review(3);

            [Lookup]
            public Product? GetProduct(int id) => new Product(id);
        }

        public class Viewer
        {
            public string Schema2 => "schema2";

            public ViewerSettings Settings => new ViewerSettings();
        }

        public class ViewerSettings
        {
            public string Schema2 => "schema2";
        }

        public record Product(int Id) : IInterface, IUnion
        {
            public string Schema2 => "schema2";

            public SharedProduct? Shared => new SharedProduct();
        }

        public record Review(int Id) : IInterface, IUnion
        {
            public string Schema2 => "schema2";
        }

        public class SharedProduct
        {
            public string Schema2 => "schema2";

            public SharedProduct2 ? Shared2 => new SharedProduct2();
        }

        public class SharedProduct2
        {
            public string Schema2 => "schema2";
        }

        public interface IInterface
        {
            int Id { get; }
        }

        [UnionType]
        public interface IUnion;
    }

    public static class SourceSchema3
    {
        public class Query
        {
            public Viewer Viewer => new Viewer();
        }

        public class Viewer
        {
            public string Schema3 => "schema3";
        }
    }
}
