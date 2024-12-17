using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[CollectionDefinition("Elastic Tests")]
public class ElasticSearchFixture : ICollectionFixture<ElasticsearchResource>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
