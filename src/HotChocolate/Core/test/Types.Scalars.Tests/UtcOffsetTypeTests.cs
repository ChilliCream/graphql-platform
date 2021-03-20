using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class UtcOffsetTypeTests : ScalarTypeTestBase
    {
        [Fact]
        protected void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<UtcOffsetType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }
    }
}
