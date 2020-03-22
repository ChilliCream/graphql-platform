using Squadron;
using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SelectionAttributeTests
        : SelectionAttributeTestsBase, IClassFixture<MongoResource>
    {
        public SelectionAttributeTests(MongoResource provider)
            : base(new MongoProvider(provider), true)
        {
        }
    }
}
