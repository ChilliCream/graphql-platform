using Squadron;
using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultAttributeTests
        : SingleOrDefaultAttributeTestsBase
        , IClassFixture<MongoResource>
    {
        public SingleOrDefaultAttributeTests(MongoResource provider)
            : base(new MongoProvider(provider), true)
        {
        }
    }
}
