using HotChocolate.Types;
using HotChocolate.Execution;

namespace HotChocolate.Text.Json;

public class ResultDocumentListRootTests
{
    [Fact]
    public void Path_Should_ContinueAtStartIndex_When_ItemsOfRootList()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        using var document = StreamBatchDocumentFactory.Create(
            operation,
            startIndex: 5,
            itemCount: 3,
            characterCount: 0);

        // act
        var paths = document.Data.GetProperty("films").EnumerateArray().Select(t => t.Path.Print());

        // assert
        paths.MatchInlineSnapshots(["films[5]", "films[6]", "films[7]"]);
    }

    [Fact]
    public void Path_Should_CountFromZero_When_ListNestedInItem()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        using var document = StreamBatchDocumentFactory.Create(
            operation,
            startIndex: 5,
            itemCount: 2,
            characterCount: 2);

        // act
        var paths = document.Data.GetProperty("films").EnumerateArray()
            .SelectMany(t => t.GetProperty("characters").EnumerateArray())
            .Select(t => t.Path.Print());

        // assert
        paths.MatchInlineSnapshots(
            [
                "films[5].characters[0]",
                "films[5].characters[1]",
                "films[6].characters[0]",
                "films[6].characters[1]"
            ]);
    }

    [Fact]
    public void Path_Should_BeRelativeToTheParentPath_When_RootListHasParentPath()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        var selection = operation.RootSelectionSet.Selections[0];
        using var document = new ResultDocument(
            CommonTestExtensions.CreateArena(),
            operation,
            selection,
            Path.Root.Append("parent"),
            3,
            new ConditionFlags(0));
        var list = document.Data.GetProperty("films");
        list.SetArrayValue(1);

        // act
        var paths = new[] { list.Path, list[0].Path };

        // assert
        paths.Select(t => t.Print()).MatchInlineSnapshots(["parent.films", "parent.films[3]"]);
    }

    [Fact]
    public void Path_Should_StartAtZero_When_StartIndexIsZero()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        using var document = StreamBatchDocumentFactory.Create(
            operation,
            startIndex: 0,
            itemCount: 2,
            characterCount: 1);

        // act
        var paths = document.Data.GetProperty("films").EnumerateArray()
            .SelectMany(t => new[] { t.Path, t.GetProperty("characters")[0].Path })
            .Select(t => t.Print());

        // assert
        paths.MatchInlineSnapshots(
            [
                "films[0]",
                "films[0].characters[0]",
                "films[1]",
                "films[1].characters[0]"
            ]);
    }

    [Fact]
    public void Type_Should_ResolveFromTheSelection_When_ElementsOfRootList()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        using var document = StreamBatchDocumentFactory.Create(
            operation,
            startIndex: 5,
            itemCount: 1,
            characterCount: 1);
        var list = document.Data.GetProperty("films");
        var item = list[0];
        var characters = item.GetProperty("characters");

        // act
        var types = new[] { list.Type, item.Type, characters.Type, characters[0].Type };

        // assert
        types.Select(t => t!.Print()).MatchInlineSnapshots(["[Film!]", "Film!", "[String]", "String"]);
    }

    [Fact]
    public void IsNullable_Should_ResolveFromTheSelection_When_ElementsOfRootList()
    {
        // arrange
        var operation = StreamBatchDocumentFactory.CreateOperation();
        using var document = StreamBatchDocumentFactory.Create(
            operation,
            startIndex: 5,
            itemCount: 1,
            characterCount: 1);
        var list = document.Data.GetProperty("films");
        var item = list[0];
        var characters = item.GetProperty("characters");

        // act
        var nullability = new[] { list.IsNullable, item.IsNullable, characters.IsNullable, characters[0].IsNullable };

        // assert
        nullability.MatchInlineSnapshot(
            """
            [
              true,
              false,
              true,
              true
            ]
            """);
    }
}
