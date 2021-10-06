using Xunit;

namespace HotChocolate.Data
{

    [CollectionDefinition("nhibernate-integration")]
    public class IntegrationCollection : ICollectionFixture<AuthorFixture>
    {

    }
}
