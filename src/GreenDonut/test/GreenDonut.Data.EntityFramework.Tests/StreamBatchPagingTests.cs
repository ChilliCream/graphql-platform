#if NET9_0_OR_GREATER
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using CookieCrumble.Resources;
using GreenDonut.Data.Cursors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamBatchPagingTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnOneFlatQueryAndEmptyPage_When_ForwardFirstIncludesAnUnmatchedKey()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 3), ("B", 3), ("C", 3));

        await using var context = new SequentialItemContext(connectionString);
        var arguments = new PagingArguments(2);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        using var capture = new CapturePagingQueryInterceptor();
        var pages = await context.Items
            .Where(t => new[] { "A", "B", "C", "D" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        var result = new
        {
            Keys = pages.Keys.OrderBy(t => t).ToArray(),
            A = new
            {
                Items = await NamesAsync(pages["A"]),
                HasNext = await pages["A"].HasNextPageAsync(cancellationToken),
                HasPrevious = await pages["A"].HasPreviousPageAsync(cancellationToken)
            },
            B = new { Items = await NamesAsync(pages["B"]) },
            C = new { Items = await NamesAsync(pages["C"]) },
            D = new
            {
                Items = await NamesAsync(pages["D"]),
                HasNext = await pages["D"].HasNextPageAsync(cancellationToken),
                HasPrevious = await pages["D"].HasPreviousPageAsync(cancellationToken)
            }
        };

        // Assert
        Snapshot.Create().Add(result).AddSql(capture).MatchMarkdownSnapshot();

        var query = Assert.Single(capture.Queries);
        Assert.Single(SplitOnOrderBy(query.QueryText));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnEachKeysLastItemsInAscendingOrder_When_PagingBackward()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 3), ("B", 1));

        await using var context = new SequentialItemContext(connectionString);
        var arguments = new PagingArguments(last: 2);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var pages = await context.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(["A-Item02", "A-Item03"], await NamesAsync(pages["A"]));
        Assert.Equal(["B-Item01"], await NamesAsync(pages["B"]));
        Assert.False(await pages["A"].HasNextPageAsync(cancellationToken));
        Assert.True(await pages["A"].HasPreviousPageAsync(cancellationToken));
        Assert.False(await pages["B"].HasPreviousPageAsync(cancellationToken));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_RunOneGroupedCountBeforeTheFlatStream_When_IncludeTotalCountIsTrue()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 5), ("B", 2));

        await using var context = new SequentialItemContext(connectionString);
        var arguments = new PagingArguments(2);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        using var capture = new CapturePagingQueryInterceptor();
        var pages = await context.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                arguments,
                includeTotalCount: true,
                cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(5, await pages["A"].TotalCountAsync(cancellationToken));
        Assert.Equal(2, await pages["B"].TotalCountAsync(cancellationToken));

        // the grouped count runs as a separate statement before the row stream opens.
        Assert.Equal(2, capture.Queries.Count);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnEmptyPage_When_RequestedKeyHasNoRows()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2));

        await using var context = new SequentialItemContext(connectionString);
        var arguments = new PagingArguments(2);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var pages = await context.Items
            .Where(t => new[] { "A", "D" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                arguments,
                includeTotalCount: true,
                cancellationToken: cancellationToken);

        // Assert
        Assert.True(pages.ContainsKey("D"));
        Assert.Empty(await NamesAsync(pages["D"]));
        Assert.Equal(0, await pages["D"].TotalCountAsync(cancellationToken));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_RunExactlyOneCommand_When_NoCountIsRequested()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 5), ("B", 2));

        var interceptor = new RecordingReaderInterceptor();
        await using var context = new SequentialItemContext(connectionString, [interceptor]);
        var arguments = new PagingArguments(2);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var pages = await context.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        await ItemsAsync(pages["A"]);
        await ItemsAsync(pages["B"]);

        // Assert
        Assert.Single(interceptor.CommandTexts);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_RunExactlyTwoCommands_When_CountIsRequested()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 5), ("B", 2));

        var interceptor = new RecordingReaderInterceptor();
        await using var context = new SequentialItemContext(connectionString, [interceptor]);
        var arguments = new PagingArguments(2);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var pages = await context.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                arguments,
                includeTotalCount: true,
                cancellationToken: cancellationToken);

        await ItemsAsync(pages["A"]);
        await ItemsAsync(pages["B"]);

        // Assert
        Assert.Equal(2, interceptor.CommandTexts.Count);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_BufferEarlierKeys_When_LastKeyIsConsumedFirst()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("C", 2));

        var interceptor = new RecordingReaderInterceptor();
        await using var context = new SequentialItemContext(connectionString, [interceptor]);
        var arguments = new PagingArguments(2);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var pages = await context.Items
            .Where(t => new[] { "A", "B", "C" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        var itemsC = await NamesAsync(pages["C"]);
        var rowsReadAfterC = interceptor.Events.Count;
        var itemsA = await NamesAsync(pages["A"]);
        var itemsB = await NamesAsync(pages["B"]);

        // Assert
        Assert.Equal(["C-Item01", "C-Item02"], itemsC);
        Assert.Equal(["A-Item01", "A-Item02"], itemsA);
        Assert.Equal(["B-Item01", "B-Item02"], itemsB);
        Assert.Equal(rowsReadAfterC, interceptor.Events.Count);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnEachKeysOwnItems_When_PagesAreConsumedInterleaved()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("C", 2));

        var interceptor = new RecordingReaderInterceptor();
        await using var context = new SequentialItemContext(connectionString, [interceptor]);
        var arguments = new PagingArguments(2);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var pages = await context.Items
            .Where(t => new[] { "A", "B", "C" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        await using var enumeratorA = pages["A"].GetAsyncEnumerator(cancellationToken);
        await using var enumeratorB = pages["B"].GetAsyncEnumerator(cancellationToken);
        await using var enumeratorC = pages["C"].GetAsyncEnumerator(cancellationToken);
        var itemsA = new List<string>();
        var itemsB = new List<string>();
        var itemsC = new List<string>();

        // advance one step per key, round-robin, instead of draining one key before the next.
        while (await enumeratorA.MoveNextAsync())
        {
            itemsA.Add(enumeratorA.Current.Name);

            if (await enumeratorB.MoveNextAsync())
            {
                itemsB.Add(enumeratorB.Current.Name);
            }

            if (await enumeratorC.MoveNextAsync())
            {
                itemsC.Add(enumeratorC.Current.Name);
            }
        }

        // Assert
        Assert.Equal(["A-Item01", "A-Item02"], itemsA);
        Assert.Equal(["B-Item01", "B-Item02"], itemsB);
        Assert.Equal(["C-Item01", "C-Item02"], itemsC);
        Assert.Single(interceptor.CommandTexts);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ProjectItemsWhileCursorsUseTheSourceElement_When_ValueSelectorIsGiven()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2));

        await using var context = new SequentialItemContext(connectionString);
        var arguments = new PagingArguments(2);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var pages = await context.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                t => t.Name.ToUpperInvariant(),
                arguments,
                cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(["A-ITEM01", "A-ITEM02"], await ItemsAsync(pages["A"]));
        Assert.Equal(["B-ITEM01", "B-ITEM02"], await ItemsAsync(pages["B"]));

        var entries = await EntriesAsync(pages["A"]);
        Assert.NotEmpty(pages["A"].CreateCursor(entries[0]));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_MatchToStreamPageAsync_When_PagingBackwardWithABeforeCursor()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 5), ("B", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var seedContext = new SequentialItemContext(connectionString);
        var seedPage = await KeyASource(seedContext).ToStreamPageAsync(
            new PagingArguments(last: 2),
            cancellationToken: cancellationToken);
        var beforeCursor = seedPage.CreateCursor((await EntriesAsync(seedPage))[0]);

        // Act
        var arguments = new PagingArguments(last: 2) { Before = beforeCursor };
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await KeyASource(expectedContext).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Assert
        await AssertMatchesStreamPageAsync(expected, batchPages["A"], t => t.Name, cancellationToken);
    }

    private static IQueryable<SequentialItem> KeyASource(SequentialItemContext context)
        => context.Items.Where(t => t.GroupKey == "A").OrderBy(t => t.Name).ThenBy(t => t.Id);

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReportNullTotalCountAndIndex_When_PagingBackwardWithoutACount()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 6), ("B", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        var arguments = new PagingArguments(last: 2);

        // Act
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                arguments,
                includeTotalCount: false,
                cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await KeyASource(expectedContext).ToStreamPageAsync(
            arguments,
            includeTotalCount: false,
            cancellationToken: cancellationToken);

        // Assert
        Assert.Null(expected.Index);
        Assert.Null(await expected.TotalCountAsync(cancellationToken));
        await AssertMatchesStreamPageAsync(expected, batchPages["A"], t => t.Name, cancellationToken);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_MatchToStreamPageAsync_When_BackwardWithARelativeBeforeCursorAndRelativeCursorsDisabled()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 8), ("B", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var seedContext = new SequentialItemContext(connectionString);
        var relativeArguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var seedPage = await KeyASource(seedContext).ToStreamPageAsync(
            relativeArguments,
            cancellationToken: cancellationToken);
        var relativeBeforeCursor = seedPage.CreateCursor((await EntriesAsync(seedPage))[0], -1);

        // Act
        var arguments = new PagingArguments(last: 2) { Before = relativeBeforeCursor, EnableRelativeCursors = false };
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await KeyASource(expectedContext).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Assert
        await AssertMatchesStreamPageAsync(expected, batchPages["A"], t => t.Name, cancellationToken);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_MatchToStreamPageAsync_When_TheSourceCarriesAProjection()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 3), ("B", 3));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var arguments = new PagingArguments(2);

        static IQueryable<SequentialItem> Project(IQueryable<SequentialItem> filtered)
            => filtered
                .Select(t => new SequentialItem { GroupKey = t.GroupKey, Name = t.Name })
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id);

        // Act
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await Project(batchContext.Items.Where(t => new[] { "A", "B" }.Contains(t.GroupKey)))
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await Project(expectedContext.Items.Where(t => t.GroupKey == "A"))
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // Assert
        await AssertMatchesStreamPageAsync(expected, batchPages["A"], t => t.Name, cancellationToken);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnCountOnlyPages_When_IncludeItemsIsFalse()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 5), ("B", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new SequentialItemContext(connectionString);
        var arguments = new PagingArguments(2) { IncludeItems = false };

        // Act
        var batchPages = await context.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                arguments,
                includeTotalCount: true,
                cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(5, await batchPages["A"].TotalCountAsync(cancellationToken));
        Assert.Equal(2, await batchPages["B"].TotalCountAsync(cancellationToken));
        Assert.Empty(await NamesAsync(batchPages["A"]));
        Assert.False(await batchPages["A"].HasNextPageAsync(cancellationToken));
        Assert.False(await batchPages["A"].HasPreviousPageAsync(cancellationToken));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_Throw_When_IncludeItemsIsFalseWithoutACount()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new SequentialItemContext(connectionString);
        var arguments = new PagingArguments(2) { IncludeItems = false };

        // Act: validation must trip before any query runs.
        using var capture = new CapturePagingQueryInterceptor();

        async Task Act()
            => await context.Items
                .Where(t => new[] { "A" }.Contains(t.GroupKey))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(
                    t => t.GroupKey,
                    arguments,
                    includeTotalCount: false,
                    cancellationToken: cancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(Act);
        Assert.Empty(capture.Queries);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_Throw_When_BeforeIsCombinedWithARelativeAfterCursor()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 5));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new SequentialItemContext(connectionString);
        var keyASource = context.Items.Where(t => t.GroupKey == "A").OrderBy(t => t.Name).ThenBy(t => t.Id);
        var relativeArguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var forwardPage = await keyASource.ToStreamPageAsync(relativeArguments, cancellationToken: cancellationToken);
        var relativeAfter = forwardPage.CreateCursor((await EntriesAsync(forwardPage))[^1], 0);

        var backwardArguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var backwardPage = await keyASource.ToStreamPageAsync(backwardArguments, cancellationToken: cancellationToken);
        var plainBefore = backwardPage.CreateCursor((await EntriesAsync(backwardPage))[0]);

        var arguments = new PagingArguments(2) { After = relativeAfter, Before = plainBefore };

        // Act
        async Task Act()
            => await context.Items
                .Where(t => new[] { "A" }.Contains(t.GroupKey))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(Act);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_FallBackToDistinctKeysQuery_When_ContainsIsNegated()
    {
        // Arrange: a `Contains` nested under `!` never stands in for the requested key set.
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("E", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new SequentialItemContext(connectionString);
        var blocked = new[] { "E" };
        var arguments = new PagingArguments(2);

        // Act
        var pages = await context.Items
            .Where(t => !blocked.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(["A", "B"], pages.Keys.OrderBy(t => t).ToArray());
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_FallBackToDistinctKeysQuery_When_ContainsIsInsideAnOrExpression()
    {
        // Arrange: a `Contains` on one side of a `||` never stands in for the requested key set.
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("C", 1), ("D", 1));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new SequentialItemContext(connectionString);
        var ids = new[] { "C" };
        var arguments = new PagingArguments(2);

        // Act
        var pages = await context.Items
            .Where(t => ids.Contains(t.GroupKey) || t.Name.StartsWith("D-"))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(["C", "D"], pages.Keys.OrderBy(t => t).ToArray());
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_FallBackToDistinctKeysQuery_When_ContainsOperandReferencesTheQueryParameter()
    {
        // Arrange: the `Contains` receiver reads a member of the query parameter itself.
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("C", 1));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new SequentialItemContext(connectionString);
        var arguments = new PagingArguments(2);

        // Act
        using var capture = new CapturePagingQueryInterceptor();
        var pages = await context.Items
            .Where(t => t.Name.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        // Assert: one distinct-keys statement, then the flat query.
        Assert.Equal(["A", "B", "C"], pages.Keys.OrderBy(t => t).ToArray());
        Assert.Equal(2, capture.Queries.Count);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_MatchToStreamPageAsync_When_TheRelativeEntryPointKeyIsEmpty()
    {
        // Arrange: key D matches no rows at all.
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 3));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };

        // Act
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A", "D" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await expectedContext.Items.Where(t => t.GroupKey == "D")
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // Assert
        await AssertMatchesStreamPageAsync(expected, batchPages["D"], t => t.Name, cancellationToken);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_MatchToStreamPageAsync_When_ARelativeAfterCursorKeyIsEmpty()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 4));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var seedContext = new SequentialItemContext(connectionString);
        var relativeArguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var seedPage = await KeyASource(seedContext).ToStreamPageAsync(
            relativeArguments,
            cancellationToken: cancellationToken);
        var afterCursor = seedPage.CreateCursor((await EntriesAsync(seedPage))[^1], 0);

        // Act
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true, After = afterCursor };
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A", "C" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                arguments,
                includeTotalCount: true,
                cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await expectedContext.Items.Where(t => t.GroupKey == "C")
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, includeTotalCount: true, cancellationToken: cancellationToken);

        // Assert
        await AssertMatchesStreamPageAsync(expected, batchPages["C"], t => t.Name, cancellationToken);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_RunExactlyOneCommand_When_ARelativeAfterCursorHasNoCountRequested()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 4));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var seedContext = new SequentialItemContext(connectionString);
        var relativeArguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var seedPage = await KeyASource(seedContext).ToStreamPageAsync(
            relativeArguments,
            cancellationToken: cancellationToken);
        var afterCursor = seedPage.CreateCursor((await EntriesAsync(seedPage))[^1], 0);

        // Act
        using var capture = new CapturePagingQueryInterceptor();
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true, After = afterCursor };
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);
        await NamesAsync(batchPages["A"]);

        // Assert
        Assert.Single(capture.Queries);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_MatchToStreamPageAsync_When_AnEndCursorKeyIsBeforeItsFirstPage()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 6), ("B", 1));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var arguments = new PagingArguments(last: 2) { Before = CursorFormatter.FormatEndCursor(-1, 6) };

        // Act
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await expectedContext.Items.Where(t => t.GroupKey == "B")
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // Assert
        await AssertMatchesStreamPageAsync(expected, batchPages["B"], t => t.Name, cancellationToken);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_MatchToStreamPageAsync_When_ForwardWithAPlainAfterCursorAndAnEmptyKey()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 4));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var seedContext = new SequentialItemContext(connectionString);
        var seedPage = await KeyASource(seedContext).ToStreamPageAsync(
            new PagingArguments(3),
            cancellationToken: cancellationToken);
        var plainAfterCursor = seedPage.CreateCursor((await EntriesAsync(seedPage))[^1]);

        // Act
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true, After = plainAfterCursor };
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A", "D" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                arguments,
                includeTotalCount: true,
                cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await expectedContext.Items.Where(t => t.GroupKey == "D")
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, includeTotalCount: true, cancellationToken: cancellationToken);

        // Assert
        await AssertMatchesStreamPageAsync(expected, batchPages["D"], t => t.Name, cancellationToken);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_MatchToStreamPageAsync_When_BackwardWithAPlainAfterCursorAndAnEmptyKey()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 4));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var seedContext = new SequentialItemContext(connectionString);
        var seedPage = await KeyASource(seedContext).ToStreamPageAsync(
            new PagingArguments(3),
            cancellationToken: cancellationToken);
        var plainAfterCursor = seedPage.CreateCursor((await EntriesAsync(seedPage))[^1]);

        // Act
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true, After = plainAfterCursor };
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A", "D" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                arguments,
                includeTotalCount: true,
                cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await expectedContext.Items.Where(t => t.GroupKey == "D")
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, includeTotalCount: true, cancellationToken: cancellationToken);

        // Assert
        await AssertMatchesStreamPageAsync(expected, batchPages["D"], t => t.Name, cancellationToken);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_MatchToStreamPageAsync_When_NeitherFirstNorLastIsGivenWithARelativeAfterCursor()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 15));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var seedContext = new SequentialItemContext(connectionString);
        var relativeArguments = new PagingArguments { EnableRelativeCursors = true };
        var seedPage = await KeyASource(seedContext).ToStreamPageAsync(
            relativeArguments,
            cancellationToken: cancellationToken);
        var relativeAfterCursor = seedPage.CreateCursor((await EntriesAsync(seedPage))[^1], 0);

        // Act
        var arguments = new PagingArguments { EnableRelativeCursors = true, After = relativeAfterCursor };
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await KeyASource(expectedContext).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Assert
        await AssertMatchesStreamPageAsync(expected, batchPages["A"], t => t.Name, cancellationToken);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_Throw_When_ARelativeAfterCursorHasANegativeOffsetWithoutFirst()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 4));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var seedContext = new SequentialItemContext(connectionString);
        var relativeArguments = new PagingArguments { EnableRelativeCursors = true };
        var seedPage = await KeyASource(seedContext).ToStreamPageAsync(
            relativeArguments,
            cancellationToken: cancellationToken);
        var relativeAfterCursor = seedPage.CreateCursor((await EntriesAsync(seedPage))[^1], -1);
        var arguments = new PagingArguments { EnableRelativeCursors = true, After = relativeAfterCursor };

        // Act
        async Task SingleAct()
        {
            await using var context = new SequentialItemContext(connectionString);
            await KeyASource(context).ToStreamPageAsync(arguments, cancellationToken: cancellationToken);
        }

        async Task BatchAct()
        {
            await using var context = new SequentialItemContext(connectionString);
            await context.Items
                .Where(t => new[] { "A" }.Contains(t.GroupKey))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);
        }

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(SingleAct);
        await Assert.ThrowsAsync<ArgumentException>(BatchAct);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_MatchToStreamPageAsync_When_APlainAfterCursorAndARelativeBeforeCursorLeaveAnEmptyWindow()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 6));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var afterSeedContext = new SequentialItemContext(connectionString);
        var afterSeedPage = await KeyASource(afterSeedContext).ToStreamPageAsync(
            new PagingArguments(3),
            cancellationToken: cancellationToken);
        var plainAfterCursor = afterSeedPage.CreateCursor((await EntriesAsync(afterSeedPage))[^1]);

        await using var beforeSeedContext = new SequentialItemContext(connectionString);
        var relativeArguments = new PagingArguments(1) { EnableRelativeCursors = true };
        var beforeSeedPage = await KeyASource(beforeSeedContext).ToStreamPageAsync(
            relativeArguments,
            cancellationToken: cancellationToken);
        var relativeBeforeCursor = beforeSeedPage.CreateCursor((await EntriesAsync(beforeSeedPage))[0], 0);

        // Act
        var arguments = new PagingArguments(2)
        {
            EnableRelativeCursors = true,
            After = plainAfterCursor,
            Before = relativeBeforeCursor
        };
        await using var batchContext = new SequentialItemContext(connectionString);
        var batchPages = await batchContext.Items
            .Where(t => new[] { "A" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                arguments,
                includeTotalCount: true,
                cancellationToken: cancellationToken);
        await using var expectedContext = new SequentialItemContext(connectionString);
        var expected = await KeyASource(expectedContext).ToStreamPageAsync(
            arguments,
            includeTotalCount: true,
            cancellationToken: cancellationToken);

        // Assert
        await AssertMatchesStreamPageAsync(expected, batchPages["A"], t => t.Name, cancellationToken);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnPageForEveryRequestedKey_When_ContainsOperandIsAnArray()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("C", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var requestedKeys = new[] { "A", "B", "C", "D" };

        // Act
        await using var context = new SequentialItemContext(connectionString);
        var pages = await context.Items
            .Where(t => requestedKeys.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, new PagingArguments(2), cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(requestedKeys.OrderBy(k => k), pages.Keys.OrderBy(k => k));
        Assert.Empty(await NamesAsync(pages["D"]));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnPageForEveryRequestedKey_When_ContainsOperandIsAList()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("C", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var requestedKeys = new List<string> { "A", "B", "C", "D" };

        // Act
        await using var context = new SequentialItemContext(connectionString);
        var pages = await context.Items
            .Where(t => requestedKeys.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, new PagingArguments(2), cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(requestedKeys.OrderBy(k => k), pages.Keys.OrderBy(k => k));
        Assert.Empty(await NamesAsync(pages["D"]));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnPageForEveryRequestedKey_When_ContainsOperandIsAHashSet()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("C", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var requestedKeys = new HashSet<string> { "A", "B", "C", "D" };

        // Act
        await using var context = new SequentialItemContext(connectionString);
        var pages = await context.Items
            .Where(t => requestedKeys.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, new PagingArguments(2), cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(requestedKeys.OrderBy(k => k), pages.Keys.OrderBy(k => k));
        Assert.Empty(await NamesAsync(pages["D"]));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnPageForEveryRequestedKey_When_ContainsOperandIsAnImmutableArray()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("C", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var requestedKeys = ImmutableArray.Create("A", "B", "C", "D");

        // Act
        await using var context = new SequentialItemContext(connectionString);
        var pages = await context.Items
            .Where(t => requestedKeys.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, new PagingArguments(2), cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(requestedKeys.OrderBy(k => k), pages.Keys.OrderBy(k => k));
        Assert.Empty(await NamesAsync(pages["D"]));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnPageForEveryRequestedKey_When_ContainsOperandIsAUnion()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("C", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var keysA = new[] { "A", "B" };
        var keysB = new[] { "C", "D" };

        // Act
        await using var context = new SequentialItemContext(connectionString);
        var pages = await context.Items
            .Where(t => keysA.Union(keysB).Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, new PagingArguments(2), cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(new[] { "A", "B", "C", "D" }, pages.Keys.OrderBy(k => k));
        Assert.Empty(await NamesAsync(pages["D"]));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnPageForEveryRequestedKey_When_ContainsOperandIsAWhereSelectChain()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("C", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var allKeys = new[] { "A", "B", "C", "D", "E" };

        // Act
        await using var context = new SequentialItemContext(connectionString);
        var pages = await context.Items
            .Where(t => allKeys.Where(k => k != "E").Select(k => k).Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, new PagingArguments(2), cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(new[] { "A", "B", "C", "D" }, pages.Keys.OrderBy(k => k));
        Assert.Empty(await NamesAsync(pages["D"]));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnPageForEveryRequestedKey_When_ContainsOperandIsASkip()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 2), ("B", 2), ("C", 2));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var groupKeys = new[] { "X", "Y", "A", "B", "C", "D" };

        // Act
        await using var context = new SequentialItemContext(connectionString);
        var pages = await context.Items
            .Where(t => groupKeys.Skip(2).Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, new PagingArguments(2), cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(new[] { "A", "B", "C", "D" }, pages.Keys.OrderBy(k => k));
        Assert.Empty(await NamesAsync(pages["D"]));
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReturnEmptyDictionary_When_TheContainsKeySetIsEmpty()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 3));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var emptyKeys = Array.Empty<string>();

        // Act
        using var capture = new CapturePagingQueryInterceptor();
        await using var context = new SequentialItemContext(connectionString);
        var pages = await context.Items
            .Where(t => emptyKeys.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.GroupKey,
                new PagingArguments(2),
                includeTotalCount: true,
                cancellationToken: cancellationToken);

        // Assert
        Assert.Empty(pages);
        Assert.Empty(capture.Queries);
    }

    private static async ValueTask<T[]> ItemsAsync<T>(StreamPage<T> page)
    {
        var items = new List<T>();

        await foreach (var item in page)
        {
            items.Add(item);
        }

        return [.. items];
    }

    private static async ValueTask<string[]> NamesAsync(StreamPage<SequentialItem> page)
        => (await ItemsAsync(page)).Select(t => t.Name).ToArray();

    private static async ValueTask<List<PageEntry<T>>> EntriesAsync<T>(StreamPage<T> page)
    {
        var entries = new List<PageEntry<T>>();

        await foreach (var entry in page.GetEntriesAsync())
        {
            entries.Add(entry);
        }

        return entries;
    }

    // Asserts that a batch key's page matches ToStreamPageAsync run against that same key's own
    // filtered source with the same arguments.
    private static async Task AssertMatchesStreamPageAsync<T>(
        StreamPage<T> expected,
        StreamPage<T> actual,
        Func<T, string> nameSelector,
        CancellationToken cancellationToken)
    {
        var expectedNames = (await EntriesAsync(expected)).Select(e => nameSelector(e.Item)).ToArray();
        var actualNames = (await EntriesAsync(actual)).Select(e => nameSelector(e.Item)).ToArray();

        Assert.Equal(expectedNames, actualNames);
        Assert.Equal(expected.Index, actual.Index);
        Assert.Equal(
            await expected.TotalCountAsync(cancellationToken),
            await actual.TotalCountAsync(cancellationToken));
        Assert.Equal(
            await expected.HasNextPageAsync(cancellationToken),
            await actual.HasNextPageAsync(cancellationToken));
        Assert.Equal(
            await expected.HasPreviousPageAsync(cancellationToken),
            await actual.HasPreviousPageAsync(cancellationToken));
    }

    private static string[] SplitOnOrderBy(string sql)
    {
        // Splits on each outer ORDER BY clause, distinct from one nested inside a window
        // function's OVER(...) clause.
        var parts = new List<string>();
        var start = 0;
        var index = sql.IndexOf("\nORDER BY", start, StringComparison.Ordinal);

        while (index >= 0)
        {
            parts.Add(sql[index..]);
            start = index + 1;
            index = sql.IndexOf("\nORDER BY", start, StringComparison.Ordinal);
        }

        return [.. parts];
    }

    private static async Task SeedAsync(string connectionString, params (string GroupKey, int Count)[] groups)
    {
        await using var context = new SequentialItemContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        foreach (var (groupKey, count) in groups)
        {
            for (var i = 1; i <= count; i++)
            {
                context.Items.Add(new SequentialItem { GroupKey = groupKey, Name = $"{groupKey}-Item{i:D2}" });
            }
        }

        await context.SaveChangesAsync();
    }

    public class SequentialItemContext(
        string connectionString,
        IEnumerable<IInterceptor>? interceptors = null) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString);

            if (interceptors is not null)
            {
                optionsBuilder.AddInterceptors(interceptors);
            }
        }

        public DbSet<SequentialItem> Items => Set<SequentialItem>();
    }

    public class SequentialItem
    {
        public int Id { get; set; }

        [MaxLength(50)] public required string GroupKey { get; set; }

        [MaxLength(100)] public required string Name { get; set; }
    }
}
#endif
