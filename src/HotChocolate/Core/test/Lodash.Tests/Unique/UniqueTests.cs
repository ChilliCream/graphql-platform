using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Lodash.Uniq
{
    public class UniqueTests : LodashTestBase
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
        [InlineData("By_OnDeepList")]
        [InlineData("By_OnDeepObject")]
        [InlineData("By_OnList")]
        [InlineData("By_OnListMissingProperty")]
        [InlineData("By_OnObjectListMixedWithList")]
        [InlineData("By_OnObjectListMixedWithScalar")]
        [InlineData("By_OnListWithNullValues")]
        [InlineData("By_OnNestedList")]
        [InlineData("By_OnScalar")]
        [InlineData("By_OnScalarList")]
        [InlineData("By_OnSingle")]
        [InlineData("By_OnSingleMissingProperty")]
        [InlineData("By_OnSingleWithNullValues")]
        public async Task ExecuteTest(string definition)
        {
            await RunTestByDefinition(definition);
        }
    }
}
