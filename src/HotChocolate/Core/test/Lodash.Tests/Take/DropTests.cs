using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Lodash.Take
{
    public class TakeTests : LodashTestBase
    {
        [Theory]
        [InlineData("OnDeepList")]
        [InlineData("OnDeepObject")]
        [InlineData("OnList")]
        [InlineData("OnListMissingProperty")]
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
