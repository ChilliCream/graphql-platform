using HotChocolate.Fusion.Suites.NestedProvides.AllProducts;
using HotChocolate.Fusion.Suites.NestedProvides.Category;
using HotChocolate.Fusion.Suites.NestedProvides.Subcategories;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("nested-provides")]
public sealed class NestedProvidesTests
    : OfficialV2ComplianceTestBase<NestedProvidesTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (AllProductsSubgraph.Name, AllProductsSubgraph.BuildAsync),
            (CategorySubgraph.Name, CategorySubgraph.BuildAsync),
            (SubcategoriesSubgraph.Name, SubcategoriesSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}
