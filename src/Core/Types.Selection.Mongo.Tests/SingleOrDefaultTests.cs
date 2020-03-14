using Squadron;
using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultTests
        : SingleOrDefaultTestsBase
        , IClassFixture<MongoResource>
    {
        public SingleOrDefaultTests(MongoResource provider)
            : base(new MongoProvider(provider))
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_Nested_Single()
        {

        }
    }
}
