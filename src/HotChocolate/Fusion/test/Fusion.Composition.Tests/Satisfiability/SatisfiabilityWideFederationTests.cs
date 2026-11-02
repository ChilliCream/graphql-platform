using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityWideFederationTests
{
    [Fact]
    public void Validate_Should_Succeed_When_SixtyThreeSchemasExtendEntityBelowCategoryTree()
    {
        // arrange
        var log = new CompositionLog();
        var validator = CreateValidator(CreateCategoryTreeSchemas(entitySchemaCount: 63), log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Validate_Should_Fail_When_SixtyThreeSchemasDeclareFieldsBelowLoopExit()
    {
        // arrange
        var log = new CompositionLog();
        var validator = CreateValidator(CreateLoopExitSchemas(fieldSchemaCount: 63), log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        var snapshot = Snapshot.Create();

        foreach (var entry in log)
        {
            snapshot.Add(entry.Message);
        }

        snapshot.MatchMarkdownSnapshot();
    }

    private static SatisfiabilityValidator CreateValidator(
        SourceSchemaText[] sourceSchemas,
        CompositionLog log)
    {
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(sourceSchemas),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };

        return new SatisfiabilityValidator(merger.Merge().Value, log, options);
    }

    private static SourceSchemaText[] CreateCategoryTreeSchemas(int entitySchemaCount)
    {
        const string core =
            """
            type Query {
                categories: [Category] @shareable
                productById(id: ID!): Product @lookup
            }

            type Category {
                id: ID! @shareable
                name: String @shareable
                children: [Category] @shareable
                products: [Product] @shareable
            }

            type Product {
                id: ID! @shareable
                reviews: [Review] @shareable
            }

            type Review {
                id: ID! @shareable
                author: User @shareable
            }

            type User {
                id: ID! @shareable
                name: String @shareable
            }
            """;

        var sourceSchemas = new SourceSchemaText[entitySchemaCount + 2];
        sourceSchemas[0] = new SourceSchemaText("S00", core);
        sourceSchemas[1] = new SourceSchemaText("S01", core);

        for (var i = 0; i < entitySchemaCount; i++)
        {
            sourceSchemas[i + 2] = new SourceSchemaText(
                $"S{i + 2:D2}",
                $$"""
                type Query {
                    userById(id: ID!): User @lookup @internal
                }

                type User {
                    id: ID! @shareable
                    orders{{i:D2}}: [Order]
                }

                type Order {
                    id: ID! @shareable
                    total: Int @shareable
                }
                """);
        }

        return sourceSchemas;
    }

    private static SourceSchemaText[] CreateLoopExitSchemas(int fieldSchemaCount)
    {
        var sourceSchemas = new SourceSchemaText[fieldSchemaCount + 2];
        sourceSchemas[0] = new SourceSchemaText(
            "S00",
            """
            type Query {
                root: M @shareable
                ping: String @shareable
            }

            type M {
                m: L @shareable
            }

            type L {
                x: String @shareable
                l: L @shareable
                dropB: L
                e: W @shareable
            }

            type W {
                id: ID! @shareable
                y: Y
            }

            type Y {
                k: String @shareable
            }

            type Z {
                k: String @shareable
            }
            """);
        sourceSchemas[1] = new SourceSchemaText(
            "S01",
            """
            type Query {
                root: M @shareable
                ping: String @shareable
                wById(id: ID!): W @lookup
            }

            type M {
                m: L @shareable
            }

            type L {
                x: String @shareable
                l: L @shareable
                e: W @shareable
            }

            type W {
                id: ID! @shareable
            }
            """);

        for (var i = 0; i < fieldSchemaCount; i++)
        {
            sourceSchemas[i + 2] = new SourceSchemaText(
                $"S{i + 2:D2}",
                $$"""
                type Query {
                    ping: String @shareable
                }

                type Y {
                    k: String @shareable
                    p{{i:D2}}: Z
                }

                type Z {
                    k: String @shareable
                }
                """);
        }

        return sourceSchemas;
    }
}
