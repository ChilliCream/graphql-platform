using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SelectionSetIndexBuilderTests
{
    [Fact]
    public void OnMerge_Should_AdvanceNextId_When_FieldSelectionSetsAreUnregistered()
    {
        // arrange
        var builder = CreateBuilder();
        var fields = GetRootFields(
            """
            {
                a {
                    id
                }
                b {
                    id
                }
                c {
                    id
                }
            }
            """);

        // act
        builder.OnMerge(fields.Take(2));
        builder.Register(fields[2].SelectionSet!);

        var mergedId = builder.GetId(fields[0].SelectionSet!);
        var registeredId = builder.GetId(fields[2].SelectionSet!);

        // assert
        Assert.Equal(mergedId, builder.GetId(fields[1].SelectionSet!));
        Assert.NotEqual(mergedId, registeredId);
        Assert.True(builder.TryGetSelectionSet(mergedId, out var mergedSelectionSet));
        Assert.True(builder.TryGetSelectionSet(registeredId, out var registeredSelectionSet));
        Assert.Same(fields[0].SelectionSet, mergedSelectionSet);
        Assert.Same(fields[2].SelectionSet, registeredSelectionSet);
    }

    [Fact]
    public void OnMerge_Should_AdvanceNextId_When_SelectionSetsAreUnregistered()
    {
        // arrange
        var builder = CreateBuilder();
        var fields = GetRootFields(
            """
            {
                a {
                    id
                }
                b {
                    id
                }
                c {
                    id
                }
            }
            """);

        // act
        builder.OnMerge(fields.Take(2).Select(t => t.SelectionSet!));
        builder.Register(fields[2].SelectionSet!);

        var mergedId = builder.GetId(fields[0].SelectionSet!);
        var registeredId = builder.GetId(fields[2].SelectionSet!);

        // assert
        Assert.Equal(mergedId, builder.GetId(fields[1].SelectionSet!));
        Assert.NotEqual(mergedId, registeredId);
        Assert.True(builder.TryGetSelectionSet(mergedId, out var mergedSelectionSet));
        Assert.True(builder.TryGetSelectionSet(registeredId, out var registeredSelectionSet));
        Assert.Same(fields[0].SelectionSet, mergedSelectionSet);
        Assert.Same(fields[2].SelectionSet, registeredSelectionSet);
    }

    private static SelectionSetIndexBuilder CreateBuilder()
    {
        var operation = GetOperation(
            """
            {
                seed {
                    id
                }
            }
            """);

        return SelectionSetIndexer.Create(operation).ToBuilder();
    }

    private static IReadOnlyList<FieldNode> GetRootFields(string document)
        => GetOperation(document)
            .SelectionSet
            .Selections
            .OfType<FieldNode>()
            .ToArray();

    private static OperationDefinitionNode GetOperation(string document)
        => Utf8GraphQLParser
            .Parse(document)
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
}
