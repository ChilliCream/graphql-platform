using ChilliCream.Nitro.CommandLine.Tui.Memory;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Memory;

public sealed class MemoryRowBadgeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MemoryRow CuratedRow(
        string id = "m-1",
        string type = "fact",
        IReadOnlyList<string>? tags = null,
        string body = "Body.",
        DateTimeOffset? time = null)
        => new(MemoryCollectionFilter.Curated, id, type, tags ?? [], body, time ?? s_now);

    private static MemoryRow JournalRow(
        string id = "j-1", string body = "Body.", DateTimeOffset? time = null)
        => new(MemoryCollectionFilter.Journal, id, Type: null, Tags: [], body, time ?? s_now);

    /// <summary>
    /// The Tags column's width for a single-row <see cref="MemoryRowBadge.Widths"/>: whatever
    /// <paramref name="maxWidth"/> leaves after the prefix, Kind, Type, Age, and their gutters.
    /// </summary>
    private static int TagsWidth(MemoryRowBadge.Widths widths, int maxWidth)
        => maxWidth - 3 - (widths.Kind + widths.Type + widths.Age + 3 * 4);

    [Fact]
    public void Render_Should_ReturnEmpty_When_MaxWidthIsZero()
    {
        // arrange
        var row = CuratedRow();
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);

        // act
        var line = MemoryRowBadge.Render(row, s_now, selected: false, maxWidth: 0, widths);

        // assert
        Assert.Empty(line);
    }

    [Fact]
    public void Render_Should_ShowEveryColumn_When_MaxWidthFits()
    {
        // arrange
        const int maxWidth = 80;
        var row = CuratedRow(type: "fact", tags: ["ops"]);
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        var expected = "   "
            + "curated".PadRight(widths.Kind)
            + "    " + "fact".PadRight(widths.Type)
            + "    " + "ops".PadRight(TagsWidth(widths, maxWidth))
            + "    " + "just now".PadRight(widths.Age);
        Assert.Equal(expected, line);
    }

    [Fact]
    public void Render_Should_ShowDashForTypeAndKindJournal_When_RowIsJournal()
    {
        // arrange
        const int maxWidth = 80;
        var row = JournalRow();
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        var expected = "   "
            + "journal".PadRight(widths.Kind)
            + "    " + "-".PadRight(widths.Type)
            + "    " + "-".PadRight(TagsWidth(widths, maxWidth))
            + "    " + "just now".PadRight(widths.Age);
        Assert.Equal(expected, line);
    }

    [Fact]
    public void Render_Should_ShowDashForTags_When_TagsIsEmpty()
    {
        // arrange
        const int maxWidth = 80;
        var row = CuratedRow(tags: []);
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, maxWidth, widths));

        // assert
        var expected = "   "
            + "curated".PadRight(widths.Kind)
            + "    " + "fact".PadRight(widths.Type)
            + "    " + "-".PadRight(TagsWidth(widths, maxWidth))
            + "    " + "just now".PadRight(widths.Age);
        Assert.Equal(expected, line);
    }

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(90, "1m ago")]
    public void Render_Should_FormatAge_When_AgeIsFreshOrMinutesOld(int elapsedSeconds, string expectedAge)
    {
        // arrange
        var row = CuratedRow(time: s_now.AddSeconds(-elapsedSeconds));
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, maxWidth: 80, widths));

        // assert
        Assert.Contains(expectedAge, line, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Should_FormatAgeAsBareDate_When_RowIsAtLeastAWeekOld()
    {
        // arrange
        var time = s_now.AddDays(-8);
        var row = CuratedRow(time: time);
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);

        // act
        var line = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, maxWidth: 80, widths));

        // assert
        Assert.Contains(time.ToUniversalTime().ToString("yyyy-MM-dd"), line, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Should_DropTypeColumn_When_WidthIsTooNarrowForEveryColumn()
    {
        // arrange
        var row = CuratedRow(tags: ["ops"]);
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);
        var wide = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, maxWidth: 80, widths));
        var narrowMaxWidth = widths.Kind + 3 + widths.Age + 4;

        // act
        var narrow = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, narrowMaxWidth, widths));
        var actual = (
            WideHasType: wide.Contains("fact", StringComparison.Ordinal),
            NarrowHasType: narrow.Contains("fact", StringComparison.Ordinal),
            NarrowHasKind: narrow.Contains("curated", StringComparison.Ordinal),
            NarrowHasAge: narrow.Contains("just now", StringComparison.Ordinal));

        // assert
        Assert.Equal((true, false, true, true), actual);
    }

    [Fact]
    public void Render_Should_DropAgeColumn_When_WidthIsTooNarrowForTypeAndAge()
    {
        // arrange
        var row = CuratedRow(tags: ["ops"]);
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);
        var narrowMaxWidth = widths.Kind + 3;

        // act
        var narrow = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, narrowMaxWidth, widths));
        var actual = (
            HasKind: narrow.Contains("curated", StringComparison.Ordinal),
            HasType: narrow.Contains("fact", StringComparison.Ordinal),
            HasAge: narrow.Contains("just now", StringComparison.Ordinal));

        // assert
        Assert.Equal((true, false, false), actual);
    }

    [Fact]
    public void Render_Should_TruncateTags_When_WidthLeavesNoRoomForTheFullList()
    {
        // arrange
        var row = CuratedRow(tags: ["deploy", "staging", "rollback", "database"]);
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);
        var narrowMaxWidth = widths.Kind + 3 + widths.Type + 4 + widths.Age + 4 + 10;

        // act
        var line = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, narrowMaxWidth, widths));

        // assert
        Assert.Contains('…', line);
    }

    [Fact]
    public void RenderHeader_Should_AlignColumnsWithRow_When_WidthIsWide()
    {
        // arrange
        const int maxWidth = 80;
        var row = CuratedRow();
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);

        // act
        var header = Markup.Remove(MemoryRowBadge.RenderHeader(maxWidth, widths));
        var line = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, maxWidth, widths));
        var kindOffset = header.IndexOf("KIND", StringComparison.Ordinal);
        var typeOffset = header.IndexOf("TYPE", StringComparison.Ordinal);

        // assert
        Assert.Equal("curated", line.Substring(kindOffset, 7));
        Assert.Equal("fact", line.Substring(typeOffset, 4));
    }

    [Fact]
    public void RenderHeader_Should_DropTheSameColumnAsRow_When_WidthIsNarrow()
    {
        // arrange
        var row = CuratedRow(tags: ["ops"]);
        var widths = MemoryRowBadge.ComputeWidths([row], s_now);
        var narrowMaxWidth = widths.Kind + 3 + widths.Age + 4;

        // act
        var header = Markup.Remove(MemoryRowBadge.RenderHeader(narrowMaxWidth, widths));
        var line = Markup.Remove(MemoryRowBadge.Render(row, s_now, selected: false, narrowMaxWidth, widths));
        var actual = (
            HeaderHasType: header.Contains("TYPE", StringComparison.Ordinal),
            RowHasType: line.Contains("fact", StringComparison.Ordinal),
            HeaderHasAge: header.Contains("AGE", StringComparison.Ordinal),
            RowHasAge: line.Contains("just now", StringComparison.Ordinal));

        // assert
        Assert.Equal((false, false, true, true), actual);
    }

    [Fact]
    public void RenderRule_Should_ReturnEmpty_When_MaxWidthIsZero()
    {
        // act
        var rule = MemoryRowBadge.RenderRule(0);

        // assert
        Assert.Empty(rule);
    }

    [Fact]
    public void RenderRule_Should_FillTheGivenWidth_When_WidthIsPositive()
    {
        // act
        var rule = Markup.Remove(MemoryRowBadge.RenderRule(20));

        // assert
        Assert.Equal(new string('─', 20), rule);
    }
}
