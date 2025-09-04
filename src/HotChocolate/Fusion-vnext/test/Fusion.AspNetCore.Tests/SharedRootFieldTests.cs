using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class SharedRootFieldTests : FusionTestBase
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

    // TODO: Add tests with inlinefragments and abstract types from the root

    [Fact]
    public async Task Test()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema4.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema5.Query>());

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

    public static class SourceSchema1
    {
        public class Query
        {
            public Viewer Viewer => new Viewer();
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
    }

    public static class SourceSchema2
    {
        public class Query
        {
            public Viewer Viewer => new Viewer();
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

    public static class SourceSchema4
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id) => new Product(id);
        }

        public record Product(int Id)
        {
            public string Schema1 => "schema1";

            public SharedProduct? Shared => new SharedProduct();
        }

        public class SharedProduct
        {
            public string Schema1 => "schema1";
        }
    }

    public static class SourceSchema5
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById(int id) => new Product(id);
        }

        public record Product(int Id)
        {
            public string Schema2 => "schema2";

            public SharedProduct? Shared => new SharedProduct();
        }

        public class SharedProduct
        {
            public string Schema2 => "schema2";
        }
    }
}
