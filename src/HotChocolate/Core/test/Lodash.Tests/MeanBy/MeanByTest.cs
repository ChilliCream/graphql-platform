using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Lodash.MeanBy
{
    public class MeanByTest : LodashTestBase
    {
        [Theory]
        [InlineData("OnDeepList")]
        [InlineData("OnDeepObject")]
        [InlineData("OnList")]
        [InlineData("OnListMissingProperty")]
        [InlineData("OnObjectListMixedWithList")]
        [InlineData("OnObjectListMixedWithScalar")]
        [InlineData("OnListOnlyNullValues")]
        [InlineData("OnListWithNullValues")]
        [InlineData("OnNestedList")]
        [InlineData("OnScalar")]
        [InlineData("OnScalarList")]
        [InlineData("OnSingle")]
        [InlineData("OnSingleMissingProperty")]
        [InlineData("OnSingleWithNullValues")]
        public async Task ExecuteTest(string definition)
        {
            await RunTestByDefinition(definition);
        }
    }
}
