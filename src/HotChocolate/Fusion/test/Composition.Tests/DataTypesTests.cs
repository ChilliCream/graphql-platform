using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class DataTypesTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Compose_Schema_With_Data_Only()
    {
        // arrange
        var configA = new SubgraphConfiguration(
            "A",
            """
            schema {
              query: Query
            }

            type Query {
              someData: SomeData
            }

            type SomeData {
              other: OtherData
            }

            type OtherData {
              a: String
            }
            """,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5001/graphql")), },
            null);

        var configB = new SubgraphConfiguration(
            "B",
            """
            schema {
              query: Query
            }

            type Query {
              someData: SomeData
            }

            type SomeData {
              other: OtherData
            }

            type OtherData {
              b: String
            }
            """,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5002/graphql")), },
            null);

        // act
        var composer = new FusionGraphComposer(logFactory: _logFactory);
        var fusionConfig = await composer.ComposeAsync(new[] { configA, configB, });

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }
}
