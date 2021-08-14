using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Lodash.Flatten
{
    public class FlattenTests : LodashTestBase
    {
        [Theory]
        [InlineData("OnDeepList")]
        [InlineData("OnDeepObject")]
        [InlineData("OnList")]
        [InlineData("OnNestedList")]
        [InlineData("OnScalar")]
        [InlineData("OnScalarList")]
        [InlineData("OnScalarListDeep")]
        [InlineData("OnSingle")]
        [InlineData("OnSingleZeroDepth")]
        public async Task ExecuteTest(string definition)
        {
            await RunTestByDefinition(definition);
        }
    }
}
