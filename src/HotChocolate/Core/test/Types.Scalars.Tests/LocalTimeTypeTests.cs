using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class LocalTimeTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<LocalTimeType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }
    }
}
