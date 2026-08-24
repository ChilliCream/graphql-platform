using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Search;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Search;

public sealed class TaskQueryParserTests
{
    [Fact]
    public void TryParse_Should_ReturnEmptyQuery_When_InputIsBlank()
    {
        // act
        var success = TaskQueryParser.TryParse("   ", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Null(query.Text);
        Assert.Null(query.Statuses);
        Assert.Null(query.Labels);
        Assert.Null(query.Type);
        Assert.Null(query.Priority);
        Assert.Null(query.Assignee);
        Assert.False(query.Ready);
        Assert.False(query.Blocked);
        Assert.False(query.Stale);
    }

    [Fact]
    public void TryParse_Should_JoinBareWords_When_MultipleGiven()
    {
        // act
        var success = TaskQueryParser.TryParse("fix   the   parser", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal("fix the parser", query.Text);
    }

    [Fact]
    public void TryParse_Should_KeepEmbeddedSpaces_When_ValueIsQuoted()
    {
        // act
        var success = TaskQueryParser.TryParse("\"fix the parser\"", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal("fix the parser", query.Text);
    }

    [Fact]
    public void TryParse_Should_KeepEmbeddedSpaces_When_KeyValueIsQuoted()
    {
        // act
        var success = TaskQueryParser.TryParse("assignee:\"jane doe\"", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal("jane doe", query.Assignee);
    }

    [Theory]
    [InlineData("status:open", "open")]
    [InlineData("status:Open", "open")]
    [InlineData("status:in-progress", "in_progress")]
    public void TryParse_Should_NormalizeStatus(string input, string expected)
    {
        // act
        var success = TaskQueryParser.TryParse(input, out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal([expected], query.Statuses);
    }

    [Fact]
    public void TryParse_Should_AccumulateStatuses_When_RepeatedTokensGiven()
    {
        // act
        var success = TaskQueryParser.TryParse(
            "status:open status:in_progress", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal(["open", "in_progress"], query.Statuses);
    }

    [Fact]
    public void TryParse_Should_AccumulateLabels_When_RepeatedTokensGiven()
    {
        // act
        var success = TaskQueryParser.TryParse(
            "label:Backend label:Frontend", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal(["backend", "frontend"], query.Labels);
    }

    [Fact]
    public void TryParse_Should_NormalizeType()
    {
        // act
        var success = TaskQueryParser.TryParse("type:BUG", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal(TaskTypes.Bug, query.Type);
    }

    [Fact]
    public void TryParse_Should_TakeLastValue_When_TypeGivenTwice()
    {
        // act
        var success = TaskQueryParser.TryParse("type:bug type:feature", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal(TaskTypes.Feature, query.Type);
    }

    [Theory]
    [InlineData("p0", 0)]
    [InlineData("P4", 4)]
    public void TryParse_Should_SetPriority_When_BareKeywordGiven(string input, int expected)
    {
        // act
        var success = TaskQueryParser.TryParse(input, out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal(expected, query.Priority);
    }

    [Theory]
    [InlineData("priority:2", 2)]
    [InlineData("priority:p3", 3)]
    [InlineData("priority:P1", 1)]
    public void TryParse_Should_SetPriority_When_KeyValueGiven(string input, int expected)
    {
        // act
        var success = TaskQueryParser.TryParse(input, out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal(expected, query.Priority);
    }

    [Fact]
    public void TryParse_Should_TreatOutOfRangeBarePriority_AsFreeText()
    {
        // act
        var success = TaskQueryParser.TryParse("p9", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Null(query.Priority);
        Assert.Equal("p9", query.Text);
    }

    [Fact]
    public void TryParse_Should_FailWithPosition_When_PriorityValueIsInvalid()
    {
        // act
        var success = TaskQueryParser.TryParse("open priority:9", out _, out var error);

        // assert
        Assert.False(success);
        Assert.Equal(5, error.Position);
        Assert.Contains("Invalid priority '9'", error.Message);
    }

    [Fact]
    public void TryParse_Should_SetAssignee()
    {
        // act
        var success = TaskQueryParser.TryParse("assignee:alice", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal("alice", query.Assignee);
    }

    [Theory]
    [InlineData("ready")]
    [InlineData("READY")]
    public void TryParse_Should_SetReady_When_BareKeywordGiven(string input)
    {
        // act
        var success = TaskQueryParser.TryParse(input, out var query, out _);

        // assert
        Assert.True(success);
        Assert.True(query.Ready);
    }

    [Fact]
    public void TryParse_Should_SetBlocked_When_BareKeywordGiven()
    {
        // act
        var success = TaskQueryParser.TryParse("blocked", out var query, out _);

        // assert
        Assert.True(success);
        Assert.True(query.Blocked);
    }

    [Fact]
    public void TryParse_Should_SetStale_When_BareKeywordGiven()
    {
        // act
        var success = TaskQueryParser.TryParse("stale", out var query, out _);

        // assert
        Assert.True(success);
        Assert.True(query.Stale);
    }

    [Fact]
    public void TryParse_Should_TreatQuotedKeyword_AsFreeText()
    {
        // act
        var success = TaskQueryParser.TryParse("\"ready\"", out var query, out _);

        // assert
        Assert.True(success);
        Assert.False(query.Ready);
        Assert.Equal("ready", query.Text);
    }

    [Fact]
    public void TryParse_Should_CombineFreeTextAndFilters_When_Mixed()
    {
        // act
        var success = TaskQueryParser.TryParse(
            "parser bug status:open p1 label:tui assignee:alice ready",
            out var query,
            out _);

        // assert
        Assert.True(success);
        Assert.Equal("parser bug", query.Text);
        Assert.Equal(["open"], query.Statuses);
        Assert.Equal(1, query.Priority);
        Assert.Equal(["tui"], query.Labels);
        Assert.Equal("alice", query.Assignee);
        Assert.True(query.Ready);
    }

    [Fact]
    public void TryParse_Should_FailWithPosition_When_KeyIsUnknown()
    {
        // act
        var success = TaskQueryParser.TryParse("open owner:alice", out var query, out var error);

        // assert
        Assert.False(success);
        Assert.Equal(TaskQuery.Empty, query);
        Assert.Equal(5, error.Position);
        Assert.Equal("Unknown key 'owner:'.", error.Message);
    }

    [Fact]
    public void TryParse_Should_TreatQuotedUnknownKeyLookingToken_AsFreeText()
    {
        // act
        var success = TaskQueryParser.TryParse("\"owner:alice\"", out var query, out _);

        // assert
        Assert.True(success);
        Assert.Equal("owner:alice", query.Text);
    }

    [Fact]
    public void ToFilter_Should_MapPlainFields_When_NoKeywordsGiven()
    {
        // arrange
        TaskQueryParser.TryParse(
            "parser status:open type:bug p1 label:tui assignee:alice",
            out var query,
            out _);
        var now = new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);

        // act
        var filter = query.ToFilter(now);

        // assert
        Assert.Equal("parser", filter.Text);
        Assert.Equal(["open"], filter.Statuses!);
        Assert.Equal(TaskTypes.Bug, filter.Type);
        Assert.Equal(1, filter.Priority);
        Assert.Equal(["tui"], filter.Labels!);
        Assert.Equal("alice", filter.Assignee);
        Assert.False(filter.ExcludeBlocked);
        Assert.Null(filter.UpdatedBefore);
        Assert.Null(filter.DeferredVisibleAt);
        Assert.Equal(TaskOrdering.PriorityCreatedId, filter.Ordering);
    }

    [Fact]
    public void ToFilter_Should_ApplyReadyDefaults_When_ReadyKeywordGiven()
    {
        // arrange
        TaskQueryParser.TryParse("ready", out var query, out _);
        var now = new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);

        // act
        var filter = query.ToFilter(now);

        // assert
        Assert.Equal([TaskStates.Open], filter.Statuses!);
        Assert.True(filter.ExcludeBlocked);
        Assert.Equal(now, filter.DeferredVisibleAt);
        Assert.Equal(TaskOrdering.ReadyPick, filter.Ordering);
    }

    [Fact]
    public void ToFilter_Should_KeepExplicitStatuses_When_ReadyKeywordAlsoGiven()
    {
        // arrange
        TaskQueryParser.TryParse("ready status:deferred", out var query, out _);
        var now = new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);

        // act
        var filter = query.ToFilter(now);

        // assert
        Assert.Equal([TaskStates.Deferred], filter.Statuses!);
    }

    [Fact]
    public void ToFilter_Should_ApplyStaleDefaults_When_StaleKeywordGiven()
    {
        // arrange
        TaskQueryParser.TryParse("stale", out var query, out _);
        var now = new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);

        // act
        var filter = query.ToFilter(now);

        // assert
        Assert.Equal([TaskStates.Open, TaskStates.InProgress], filter.Statuses!);
        Assert.Equal(now - TaskQuery.DefaultStaleWindow, filter.UpdatedBefore);
        Assert.Equal(TaskOrdering.UpdatedAtAscending, filter.Ordering);
    }

    [Fact]
    public void ToFilter_Should_NotRepresentBlocked_Because_TaskFilterHasNoMatchingField()
    {
        // arrange
        TaskQueryParser.TryParse("blocked", out var query, out _);
        var now = new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);

        // act
        var filter = query.ToFilter(now);

        // assert
        Assert.True(query.Blocked);
        Assert.Null(filter.Statuses);
        Assert.False(filter.ExcludeBlocked);
        Assert.Equal(TaskOrdering.PriorityCreatedId, filter.Ordering);
    }
}
