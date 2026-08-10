using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Parsing;

public class FragmentReferenceFinderTests
{
    [Fact]
    public void Find_Should_Not_Report_Anchor_As_External_When_Sibling_Fragment_Spreads_It()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            fragment ApiError on Error {
              message
            }

            fragment NameTooLongError on NameTooLongError {
              ...ApiError
              attemptedName
            }
            """);
        var anchor = document.Definitions.OfType<FragmentDefinitionNode>().First();

        // act
        var result = FragmentReferenceFinder.Find(document, anchor);

        // assert
        Assert.Empty(result.External);
        var localFragment = Assert.Single(result.Local);
        Assert.Equal("NameTooLongError", localFragment.Key);
    }
}
