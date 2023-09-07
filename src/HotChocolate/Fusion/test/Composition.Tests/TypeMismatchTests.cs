using CookieCrumble;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class TypeMismatchTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

    [Fact]
    public async Task Output_Rewrite_Nullability_For_Output_Types()
        => await Succeed(
            """
            type Query {
              someData1: String!
              someData2: [String!]
              someData3: [String!]
              someData4: [String!]!
              someData5: [String!]!
              someData6: [[String!]!]
              someData7: [[String!]!]
              someData8: [[String!]!]!
              someData9: [[String!]!]!
            }
            """,
            """
            type Query {
              someData1: String
              someData2: [String]
              someData3: [String]!
              someData4: [String!]
              someData5: [String]!
              someData6: [[String!]]
              someData7: [[String]!]
              someData8: [[String!]!]
              someData9: [[String]!]!
            }
            """);

    [Fact]
    public async Task Output_Fail_On_Named_Type_Mismatch()
        => await Fail(
            """
            type Query {
              someData1: String!
            }
            """,
            """
            type Query {
              someData1: Int!
            }
            """);
    
    [Fact]
    public async Task Output_Fail_On_Structure_1_Mismatch()
        => await Fail(
            """
            type Query {
              someData1: String!
            }
            """,
            """
            type Query {
              someData1: [String]!
            }
            """);
    
    [Fact]
    public async Task Output_Fail_On_Structure_2_Mismatch()
        => await Fail(
            """
            type Query {
              someData1: [String]!
            }
            """,
            """
            type Query {
              someData1: [[String]]!
            }
            """);

    private async Task Succeed(string schemaA, string schemaB)
    {
        // arrange
        var configA = new SubgraphConfiguration(
            "A",
            schemaA,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5001/graphql")) });

        var configB = new SubgraphConfiguration(
            "B",
            schemaB,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5002/graphql")) });

        // act
        var composer = new FusionGraphComposer(logFactory: _logFactory);
        var fusionConfig = await composer.ComposeAsync(new[] { configA, configB });

        SchemaFormatter
            .FormatAsString(fusionConfig)
            .MatchSnapshot(extension: ".graphql");
    }
    
    private async Task Fail(string schemaA, string schemaB)
    {
        // arrange
        var configA = new SubgraphConfiguration(
            "A",
            schemaA,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5001/graphql")) });

        var configB = new SubgraphConfiguration(
            "B",
            schemaB,
            Array.Empty<string>(),
            new[] { new HttpClientConfiguration(new Uri("https://localhost:5002/graphql")) });

        // act
        var log = new ErrorCompositionLog();
        var composer = new FusionGraphComposer(logFactory: () => log);
        await composer.TryComposeAsync(new[] { configA, configB });

        var snapshot = new Snapshot();
        
        foreach (var error in log.Errors)
        {
            snapshot.Add(error.Message);
        }

        await snapshot.MatchAsync();
    }
}