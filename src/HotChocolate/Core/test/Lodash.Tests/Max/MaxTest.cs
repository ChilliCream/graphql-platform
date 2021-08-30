using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Lodash.Max
{
    public class MaxTest : LodashTestBase
    {
        [Theory]
        [InlineData("By_OnDeepList")]
        [InlineData("By_OnDeepObject")]
        [InlineData("By_OnList")]
        [InlineData("By_OnListMissingProperty")]
        [InlineData("By_OnObjectListMixedWithList")]
        [InlineData("By_OnObjectListMixedWithScalar")]
        [InlineData("By_OnListOnlyNullValues")]
        [InlineData("By_OnListWithNullValues")]
        [InlineData("By_OnNestedList")]
        [InlineData("By_OnScalar")]
        [InlineData("By_OnScalarList")]
        [InlineData("By_OnSingle")]
        [InlineData("By_OnSingleMissingProperty")]
        [InlineData("By_OnSingleWithNullValues")]
        [InlineData("OnDeepList")]
        [InlineData("OnDeepObject")]
        [InlineData("OnList")]
        [InlineData("OnObjectListMixedWithList")]
        [InlineData("OnObjectListMixedWithScalar")]
        [InlineData("OnListOnlyNullValues")]
        [InlineData("OnListWithNullValues")]
        [InlineData("OnNestedList")]
        [InlineData("OnScalar")]
        [InlineData("OnScalarList")]
        [InlineData("OnScalarListWithNullValues")]
        [InlineData("OnScalarListWithOnlyNullValues")]
        [InlineData("OnSingle")]
        [InlineData("OnSingleWithNullValues")]
        public async Task ExecuteTest(string definition)
        {
            await RunTestByDefinition(definition);
        }
    }
}
