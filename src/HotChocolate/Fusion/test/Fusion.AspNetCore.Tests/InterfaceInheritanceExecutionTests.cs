using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class InterfaceInheritanceExecutionTests : FusionTestBase
{
    // Ordering owns the interface hierarchy OrderBase <- MultiOrderBase <- OrderA/OrderB and a
    // Product stub (id only). Products resolves the remaining Product fields through a lookup.
    private const string OrderingSchema =
        """
        type Query {
          orders: [OrderBase]
        }

        interface OrderBase {
          name: String
        }

        interface MultiOrderBase implements OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderA implements MultiOrderBase & OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderB implements MultiOrderBase & OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderItem {
          product: Product
        }

        type Product @key(fields: "id") {
          id: ID!
        }
        """;

    private const string ProductsSchema =
        """
        type Query {
          productById(id: ID! @is(field: "id")): Product @lookup @internal
        }

        type Product @key(fields: "id") {
          id: ID!
          name: String
          description: String
        }
        """;

    // https://github.com/ChilliCream/graphql-platform/issues/10045
    // A lookup reached through an intermediate interface fragment must resolve the concrete
    // runtime type (OrderA/OrderB) against the abstract type condition (MultiOrderBase). Before
    // the executor was made subtype-aware the products fetch was skipped and name/description
    // came back null while id survived from the first fetch.
    [Fact]
    public async Task Execution_Should_ResolveProduct_When_LookupReachedThroughIntermediateInterfaceFragment()
    {
        // arrange
        using var ordering = CreateSourceSchema("ordering", OrderingSchema);
        using var products = CreateSourceSchema("products", ProductsSchema);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("ordering", ordering),
            ("products", products)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              orders {
                ... on OrderBase {
                  name
                  ... on MultiOrderBase {
                    items {
                      product { id name description }
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        var orders = response.Data.GetProperty("orders");
        Assert.Equal(JsonValueKind.Array, orders.ValueKind);

        foreach (var order in orders.EnumerateArray())
        {
            foreach (var item in order.GetProperty("items").EnumerateArray())
            {
                var product = item.GetProperty("product");
                Assert.False(string.IsNullOrEmpty(product.GetProperty("name").GetString()));
                Assert.False(string.IsNullOrEmpty(product.GetProperty("description").GetString()));
            }
        }
    }

    [Fact]
    public async Task Execution_Should_ResolveProduct_When_LookupReachedThroughDirectIntermediateInterfaceFragment()
    {
        // arrange
        using var ordering = CreateSourceSchema("ordering", OrderingSchema);
        using var products = CreateSourceSchema("products", ProductsSchema);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("ordering", ordering),
            ("products", products)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              orders {
                ... on MultiOrderBase {
                  items {
                    product { id name description }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    // Ordering hierarchy with a third interface level PremiumOrderBase to exercise subtype
    // matching across a nested interface-on-interface chain.
    private const string NestedOrderingSchema =
        """
        type Query {
          orders: [OrderBase]
        }

        interface OrderBase {
          name: String
        }

        interface MultiOrderBase implements OrderBase {
          name: String
          items: [OrderItem!]
        }

        interface PremiumOrderBase implements MultiOrderBase & OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderA implements PremiumOrderBase & MultiOrderBase & OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderB implements PremiumOrderBase & MultiOrderBase & OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderItem {
          product: Product
        }

        type Product @key(fields: "id") {
          id: ID!
        }
        """;

    [Fact]
    public async Task Execution_Should_ResolveProduct_When_LookupReachedThroughNestedInterfaceHierarchy()
    {
        // arrange
        using var ordering = CreateSourceSchema("ordering", NestedOrderingSchema);
        using var products = CreateSourceSchema("products", ProductsSchema);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("ordering", ordering),
            ("products", products)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              orders {
                ... on OrderBase {
                  ... on MultiOrderBase {
                    ... on PremiumOrderBase {
                      items {
                        product { id name description }
                      }
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    // The intermediate interface OrderBase itself carries the items field, so the lookup target
    // condition is the top-level interface while the runtime type is two levels below it.
    private const string TopInterfaceOrderingSchema =
        """
        type Query {
          orders: [OrderBase]
        }

        interface OrderBase {
          name: String
          items: [OrderItem!]
        }

        interface MultiOrderBase implements OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderA implements MultiOrderBase & OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderB implements MultiOrderBase & OrderBase {
          name: String
          items: [OrderItem!]
        }

        type OrderItem {
          product: Product
        }

        type Product @key(fields: "id") {
          id: ID!
        }
        """;

    [Fact]
    public async Task Execution_Should_ResolveProduct_When_LookupReachedThroughTopLevelInterfaceFragment()
    {
        // arrange
        using var ordering = CreateSourceSchema("ordering", TopInterfaceOrderingSchema);
        using var products = CreateSourceSchema("products", ProductsSchema);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("ordering", ordering),
            ("products", products)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              orders {
                ... on OrderBase {
                  items {
                    product { id name description }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    // Catalog exposes a union with key-only members; Details resolves the remaining member fields
    // through a union lookup. The lookup narrows its result to the concrete member via a type
    // condition, so the source path carries an inline fragment (for example $.searchResultById
    // <Book>). This exercises the source-path walker twin of the target-side matcher.
    private const string CatalogSchema =
        """
        type Query {
          search: SearchResult
        }

        union SearchResult = Book | Movie

        type Book @key(fields: "id") {
          id: ID!
        }

        type Movie @key(fields: "id") {
          id: ID!
        }
        """;

    private const string DetailsSchema =
        """
        type Query {
          searchResultById(id: ID!): SearchResult @lookup @internal
        }

        union SearchResult = Book | Movie

        type Book @key(fields: "id") {
          id: ID!
          title: String
        }

        type Movie @key(fields: "id") {
          id: ID!
          runtime: Int
        }
        """;

    [Fact]
    public async Task Execution_Should_ResolveUnionMemberField_When_LookupNarrowsSourcePathByTypeCondition()
    {
        // arrange
        using var catalog = CreateSourceSchema("catalog", CatalogSchema);
        using var details = CreateSourceSchema("details", DetailsSchema);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("catalog", catalog),
            ("details", details)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              search {
                ... on Book {
                  id
                  title
                }
                ... on Movie {
                  id
                  runtime
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }
}
