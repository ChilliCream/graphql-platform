using CookieCrumble;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Shared;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class SemanticNonNullComposeTests(ITestOutputHelper output)
{
    # region SemantionNonNull & Nullable

    [Fact]
    public async Task Merge_SemanticNonNull_With_Nullable()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: SubType @semanticNonNull
            }

            type SubType {
              field: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: SubType
            }

            type SubType {
              field: String
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: SubType
            }

            type SubType {
              field: String
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_Nullable_With_SemanticNonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: SubType
            }

            type SubType {
              field: String
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: SubType @semanticNonNull
            }

            type SubType {
              field: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: SubType
            }

            type SubType {
              field: String
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_List_With_Nullable_List()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_Nullable_List_With_SemanticNonNull_List()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_ListItem_With_Nullable_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_Nullable_ListItem_With_SemanticNonNull_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_List_And_ListItem_With_Nullable_List_And_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [0,1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [0,1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_Nullable_List_And_ListItem_With_SemanticNonNull_List_And_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [0,1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [0,1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType]
            }

            type SubType {
              field: [String]
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task
        Merge_SemanticNonNull_List_Nullable_List_SemanticNonNull_ListItem_With_Nullable_List_Nullable_List_Nullable_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [[SubType]] @semanticNonNull(levels: [0, 2])
            }

            type SubType {
              field: [[String]] @semanticNonNull(levels: [0, 2])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [[SubType]]
            }

            type SubType {
              field: [[String]]
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [[SubType]]
            }

            type SubType {
              field: [[String]]
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task
        Merge_Nullable_List_Nullable_List_Nullable_ListItem_With_SemanticNonNull_List_Nullable_List_SemanticNonNull_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [[SubType]]
            }

            type SubType {
              field: [[String]]
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [[SubType]] @semanticNonNull(levels: [0, 2])
            }

            type SubType {
              field: [[String]] @semanticNonNull(levels: [0, 2])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [[SubType]]
            }

            type SubType {
              field: [[String]]
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    # endregion

    #region SemanticNonNull & SemanticNonNull

    [Fact]
    public async Task Merge_SemanticNonNull_With_SemanticNonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: SubType @semanticNonNull
            }

            type SubType {
              field: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: SubType @semanticNonNull
            }

            type SubType {
              field: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: SubType @semanticNonNull
            }

            type SubType {
              field: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_List_With_SemanticNonNull_List()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_ListItem_With_SemanticNonNull_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull(levels: [ 1 ])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [ 1 ])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task
        Merge_SemanticNonNull_List_Nullable_List_SemanticNonNull_ListItem_With_SemanticNonNull_List_Nullable_List_SemanticNonNull_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [[SubType]] @semanticNonNull(levels: [0, 2])
            }

            type SubType {
              field: [[String]] @semanticNonNull(levels: [0, 2])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [[SubType]] @semanticNonNull(levels: [0, 2])
            }

            type SubType {
              field: [[String]] @semanticNonNull(levels: [0, 2])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [[SubType]] @semanticNonNull(levels: [ 0, 2 ])
            }

            type SubType {
              field: [[String]] @semanticNonNull(levels: [ 0, 2 ])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_List_And_ListItem_With_SemanticNonNull_List_And_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [0,1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [0,1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [0,1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [0,1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull(levels: [ 0, 1 ])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [ 0, 1 ])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_List_With_Nullable_And_NonNull_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType!] @semanticNonNull
            }

            type SubType {
              field: [String!] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_List_With_NonNull_And_Nullable_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType!] @semanticNonNull
            }

            type SubType {
              field: [String!] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    # endregion

    # region SemanticNonNull & NonNull

    [Fact]
    public async Task Merge_SemanticNonNull_With_NonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: SubType @semanticNonNull
            }

            type SubType {
              field: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: SubType!
            }

            type SubType {
              field: String!
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: SubType @semanticNonNull
            }

            type SubType {
              field: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_NonNull_With_SemanticNonNull()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: SubType!
            }

            type SubType {
              field: String!
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: SubType @semanticNonNull
            }

            type SubType {
              field: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: SubType @semanticNonNull
            }

            type SubType {
              field: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_List_With_NonNull_List()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType]!
            }

            type SubType {
              field: [String]!
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_NonNull_List_With_SemanticNonNull_List()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType]!
            }

            type SubType {
              field: [String]!
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull
            }

            type SubType {
              field: [String] @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_ListItem_With_NonNull_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType!]
            }

            type SubType {
              field: [String!]
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull(levels: [ 1 ])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [ 1 ])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_NonNull_ListItem_With_SemanticNonNull_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType!]
            }

            type SubType {
              field: [String!]
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull(levels: [ 1 ])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [ 1 ])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_SemanticNonNull_List_And_ListItem_With_NonNull_List_And_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [0,1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [0,1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType!]!
            }

            type SubType {
              field: [String!]!
            }
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull(levels: [ 0, 1 ])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [ 0, 1 ])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task Merge_NonNull_List_And_ListItem_With_SemanticNonNull_List_And_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType!]!
            }

            type SubType {
              field: [String!]!
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [SubType] @semanticNonNull(levels: [0,1])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [0,1])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [SubType] @semanticNonNull(levels: [ 0, 1 ])
            }

            type SubType {
              field: [String] @semanticNonNull(levels: [ 0, 1 ])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public async Task
        Merge_NonNull_List_Nullable_List_NonNull_ListItem_With_SemanticNonNull_List_Nullable_List_SemanticNonNull_ListItem()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [[SubType!]]!
            }

            type SubType {
              field: [[String!]]!
            }
            """
        );

        var subgraphB = await TestSubgraph.CreateAsync(
            """
            type Query {
              field: [[SubType]] @semanticNonNull(levels: [0, 2])
            }

            type SubType {
              field: [[String]] @semanticNonNull(levels: [0, 2])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """
        );

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);

        // act
        var fusionGraph = await subgraphs.GetFusionGraphAsync();

        // assert
        GetSchemaWithoutFusion(fusionGraph).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              field: [[SubType]] @semanticNonNull(levels: [ 0, 2 ])
            }

            type SubType {
              field: [[String]] @semanticNonNull(levels: [ 0, 2 ])
            }

            directive @semanticNonNull(levels: [Int] = [ 0 ]) on FIELD_DEFINITION
            """);
    }

    #endregion

    private static DocumentNode GetSchemaWithoutFusion(SchemaDefinition fusionGraph)
    {
        var sourceText = SchemaFormatter.FormatAsString(fusionGraph);
        var fusionGraphDoc = Utf8GraphQLParser.Parse(sourceText);
        var typeNames = FusionTypeNames.From(fusionGraphDoc);
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();

        var rewrittenDocumentNode = (DocumentNode)rewriter.Rewrite(fusionGraphDoc, new(typeNames))!;

        return rewrittenDocumentNode;
    }
}
