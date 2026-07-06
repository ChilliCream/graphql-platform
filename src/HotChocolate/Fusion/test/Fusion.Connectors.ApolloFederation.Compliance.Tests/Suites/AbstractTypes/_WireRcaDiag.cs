using HotChocolate.Execution;
using HotChocolate.Fusion.Suites.AbstractTypes.Agency;
using HotChocolate.Fusion.Suites.AbstractTypes.Books;
using HotChocolate.Fusion.Suites.AbstractTypes.Inventory;
using HotChocolate.Fusion.Suites.AbstractTypes.Magazines;
using HotChocolate.Fusion.Suites.AbstractTypes.Products;
using HotChocolate.Fusion.Suites.AbstractTypes.Reviews;
using HotChocolate.Fusion.Suites.AbstractTypes.Users;

namespace HotChocolate.Fusion.Suites;

public sealed class _WireRcaDiag
{
    private const string OutDir =
        "/private/tmp/claude-501/-Users-michael-local-hc-1-repo/dd0e966b-0c00-47c0-8d06-02fbf51d3d57/scratchpad/wire";

    private static async Task CaptureAsync(string name, string query)
    {
        System.IO.Directory.CreateDirectory(OutDir);
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder.ComposeAsync(
            capture,
            (AgencySubgraph.Name, AgencySubgraph.BuildAsync),
            (BooksSubgraph.Name, BooksSubgraph.BuildAsync),
            (InventorySubgraph.Name, InventorySubgraph.BuildAsync),
            (MagazinesSubgraph.Name, MagazinesSubgraph.BuildAsync),
            (ProductsSubgraph.Name, ProductsSubgraph.BuildAsync),
            (ReviewsSubgraph.Name, ReviewsSubgraph.BuildAsync),
            (UsersSubgraph.Name, UsersSubgraph.BuildAsync));

        var result = await gateway.Executor.ExecuteAsync(query);
        var json = result.ToJson(withIndentations: true);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("===== RESULT =====");
        sb.AppendLine(json);
        sb.AppendLine();
        sb.AppendLine("===== WIRE REQUESTS =====");
        foreach (var req in capture.Requests)
        {
            sb.AppendLine($"--- to {req.SubgraphName} ---");
            sb.AppendLine(req.Body);
            sb.AppendLine();
        }

        System.IO.File.WriteAllText(System.IO.Path.Combine(OutDir, name + ".txt"), sb.ToString());
    }

    [Fact]
    public Task Bug1_NestedProductInfo() => CaptureAsync(
        "bug1",
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
    public Task Bug2_Duplicate_SkipFalse() => CaptureAsync(
        "bug2_skipfalse",
        """
        query ($title: Boolean = false) {
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
    public Task Control_SingleBook_SkipFalse() => CaptureAsync(
        "control_skipfalse",
        """
        query ($title: Boolean = false) {
          products {
            id
            reviews { id }
            ... on Book @skip(if: $title) { title }
            ... on Magazine { sku }
          }
        }
        """);
}
