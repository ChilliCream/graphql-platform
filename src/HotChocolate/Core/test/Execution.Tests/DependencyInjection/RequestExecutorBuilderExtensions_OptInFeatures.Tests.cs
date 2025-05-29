using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.DependencyInjection;

public class RequestExecutorBuilderExtensionsOptInFeaturesTests
{
    [Fact]
    public void OptInFeatureStability_NullBuilder_ThrowsArgumentNullException()
    {
        void Fail() => RequestExecutorBuilderExtensions
            .OptInFeatureStability(null!, "feature", "stability");

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void OptInFeatureStability_NullFeature_ThrowsArgumentNullException()
    {
        void Fail() => new ServiceCollection()
            .AddGraphQL()
            .OptInFeatureStability(null!, "stability");

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void OptInFeatureStability_NullStability_ThrowsArgumentNullException()
    {
        void Fail() => new ServiceCollection()
            .AddGraphQL()
            .OptInFeatureStability("feature", null!);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task ExecuteRequestAsync_OptInFeatureStability_MatchesSnapshot()
    {
        (await new ServiceCollection()
            .AddGraphQLServer()
            .ModifyOptions(o => o.EnableOptInFeatures = true)
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .OptInFeatureStability("feature1", "stability1")
            .OptInFeatureStability("feature2", "stability2")
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument(
                        """
                        {
                            __schema {
                                optInFeatureStability {
                                    feature
                                    stability
                                }
                            }
                        }
                        """)
                    .Build()))
            .MatchInlineSnapshot(
                """
                {
                  "data": {
                    "__schema": {
                      "optInFeatureStability": [
                        {
                          "feature": "feature1",
                          "stability": "stability1"
                        },
                        {
                          "feature": "feature2",
                          "stability": "stability2"
                        }
                      ]
                    }
                  }
                }
                """);
    }
}
