using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Tests
{
    public class QueryRequestBuilderTests
    {
        [Fact]
        public void BuildRequest_OnlyQueryIsSet_RequestHasOnlyQuery()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .Create();

            // assert
            request.MatchSnapshot();
        }

        [Fact]
        public void BuildRequest_Empty_QueryRequestBuilderException()
        {
            // arrange
            // act
            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .Create();

            // assert
            request.MatchSnapshot();
        }
    }
}
