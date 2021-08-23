using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Lodash.Uniq
{
    public class UniqTests : LodashTestBase
    {
        [Theory]
        [InlineData("OnDeepList")]
        [InlineData("OnDeepObject")]
        [InlineData("OnList")]
        [InlineData("OnNestedList")]
        [InlineData("OnScalar")]
        [InlineData("OnScalarList")]
        [InlineData("OnScalarListWithNullValues")]
        [InlineData("OnSingle")]
        [InlineData("OnSingleWithNullValues")]
        public async Task ExecuteTest(string definition)
        {
            await RunTestByDefinition(definition);
        }
    }
}
