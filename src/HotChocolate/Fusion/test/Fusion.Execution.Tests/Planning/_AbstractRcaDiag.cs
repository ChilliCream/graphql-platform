using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class _AbstractRcaDiag : FusionTestBase
{
    private const string OutDir =
        "/private/tmp/claude-501/-Users-michael-local-hc-1-repo/dd0e966b-0c00-47c0-8d06-02fbf51d3d57/scratchpad/plans";

    private static void Dump(string name, FusionSchemaDefinition schema, string query)
    {
        System.IO.Directory.CreateDirectory(OutDir);
        try
        {
            var plan = PlanOperation(schema, query);
            var yaml = new YamlOperationPlanFormatter().Format(plan);
            System.IO.File.WriteAllText(System.IO.Path.Combine(OutDir, name + ".yaml"), yaml);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(OutDir, name + ".error.txt"), ex.ToString());
        }
    }

    // BUG 1: interface-level sku on the DOUBLE-nested abstract field products.reviews.product.
    // sku lives in "a" (co-located with products root), reviews in "r", title in "m".
    private static FusionSchemaDefinition Bug1Schema()
        => ComposeSchema(
            """
            # name: a
            schema { query: Query }

            type Query {
              products: [Product]
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! sku: String }
            type Book implements Product @key(fields: "id") { id: ID! sku: String }
            type Magazine implements Product @key(fields: "id") { id: ID! sku: String }
            """,
            """
            # name: r
            schema { query: Query }

            type Query {
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! reviews: [Review] reviewsCount: Int! }
            type Book implements Product @key(fields: "id") { id: ID! reviews: [Review] reviewsCount: Int! }
            type Magazine implements Product @key(fields: "id") { id: ID! reviews: [Review] reviewsCount: Int! }
            type Review { id: ID! product: Product }
            """,
            """
            # name: m
            schema { query: Query }

            type Query {
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            type Magazine @key(fields: "id") { id: ID! title: String }
            """);

    [Fact]
    public void Bug1_InterfaceLevel_Sku()
        => Dump(
            "bug1",
            Bug1Schema(),
            """
            {
              products {
                id
                reviews {
                  product {
                    sku
                    ... on Magazine { title }
                    ... on Book { reviewsCount }
                  }
                }
              }
            }
            """);

    [Fact]
    public void Bug1_ExplicitFragments_Control()
        => Dump(
            "bug1_control",
            Bug1Schema(),
            """
            {
              products {
                id
                reviews {
                  product {
                    ... on Book { sku reviewsCount }
                    ... on Magazine { sku title }
                  }
                }
              }
            }
            """);

    // BUG 2: duplicate sibling ... on Book fragments. sku co-located at root "a";
    // reviews needs an "r" lookup per concrete type; title needs a "books" lookup (Book-only).
    private static FusionSchemaDefinition Bug2Schema()
        => ComposeSchema(
            """
            # name: a
            schema { query: Query }

            type Query {
              products: [Product]
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! sku: String }
            type Book implements Product @key(fields: "id") { id: ID! sku: String }
            type Magazine implements Product @key(fields: "id") { id: ID! sku: String }
            """,
            """
            # name: r
            schema { query: Query }

            type Query {
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              magazineById(id: ID! @is(field: "id")): Magazine @lookup @internal
            }

            interface Product { id: ID! reviews: [Review] }
            type Book implements Product @key(fields: "id") { id: ID! reviews: [Review] }
            type Magazine implements Product @key(fields: "id") { id: ID! reviews: [Review] }
            type Review { id: ID! }
            """,
            """
            # name: books
            schema { query: Query }

            type Query {
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
            }

            type Book @key(fields: "id") { id: ID! title: String }
            """);

    [Fact]
    public void Bug2_DuplicateBookFragments()
        => Dump(
            "bug2",
            Bug2Schema(),
            """
            query ($title: Boolean = true) {
              products {
                id
                reviews { id }
                ... on Book @skip(if: $title) { title }
                ... on Book { sku }
                ... on Magazine { sku }
              }
            }
            """);

    [Fact]
    public void Bug2_SingleBookFragment_Control()
        => Dump(
            "bug2_control",
            Bug2Schema(),
            """
            {
              products {
                id
                reviews { id }
                ... on Book { title sku }
                ... on Magazine { sku }
              }
            }
            """);
}
