using HotChocolate.Fusion.Suites.AbstractTypes.Agency;
using HotChocolate.Fusion.Suites.AbstractTypes.Books;
using HotChocolate.Fusion.Suites.AbstractTypes.Inventory;
using HotChocolate.Fusion.Suites.AbstractTypes.Magazines;
using HotChocolate.Fusion.Suites.AbstractTypes.Products;
using HotChocolate.Fusion.Suites.AbstractTypes.Reviews;
using HotChocolate.Fusion.Suites.AbstractTypes.Users;

namespace HotChocolate.Fusion.Suites;

[OfficialV1Suite("abstract-types")]
public sealed class AbstractTypesTests : OfficialV1ComplianceTestBase<AbstractTypesTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV1Async(
            (AgencySubgraph.Name, AgencySubgraph.BuildAsync),
            (InventorySubgraph.Name, InventorySubgraph.BuildAsync),
            (BooksSubgraph.Name, BooksSubgraph.BuildAsync),
            (UsersSubgraph.Name, UsersSubgraph.BuildAsync),
            (ReviewsSubgraph.Name, ReviewsSubgraph.BuildAsync),
            (ProductsSubgraph.Name, ProductsSubgraph.BuildAsync),
            (MagazinesSubgraph.Name, MagazinesSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV1")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV1CaseAsync(caseId);
}
