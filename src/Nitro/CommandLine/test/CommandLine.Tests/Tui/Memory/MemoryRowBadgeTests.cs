using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Memory;

public sealed class MemoryRowBadgeTests
{
    [Fact]
    public void Render_Should_FitDisplayWidth_When_TagsContainCjkAndEmoji()
    {
        // arrange
        const int maxWidth = 13;
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var record = CreateRecord(type: "fact", tags: ["😀漢"]);
        var widths = MemoryRowBadge.ComputeWidths([record], now);

        // act
        var line = MemoryRowBadge.Render(record, now, selected: false, maxWidth, widths);
        var plainText = Markup.Remove(line);

        // assert
        Assert.Equal("  fact … now", plainText);
        Assert.True(plainText.GetCellWidth() <= maxWidth);
    }

    [Fact]
    public void Render_Should_FitFixedColumns_When_MaxWidthMatchesTheirDisplayWidth()
    {
        // arrange
        const int maxWidth = 11;
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var record = CreateRecord(type: "漢😀", tags: ["tag"]);
        var widths = MemoryRowBadge.ComputeWidths([record], now);

        // act
        var line = MemoryRowBadge.Render(record, now, selected: false, maxWidth, widths);
        var plainText = Markup.Remove(line);

        // assert
        Assert.Equal("  漢😀  now", plainText);
        Assert.Equal(maxWidth, plainText.GetCellWidth());
    }

    [Fact]
    public void Render_Should_FitWithinOneColumn_When_Selected()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var record = CreateRecord(type: "😀", tags: ["漢"]);
        var widths = MemoryRowBadge.ComputeWidths([record], now);

        // act
        var line = MemoryRowBadge.Render(record, now, selected: true, maxWidth: 1, widths);
        var plainText = Markup.Remove(line);

        // assert
        Assert.Equal(">", plainText);
    }

    [Fact]
    public void Render_Should_PreserveNormalOutput_When_ItFits()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var record = CreateRecord(type: "fact", tags: ["ops"]);
        var widths = MemoryRowBadge.ComputeWidths([record], now);

        // act
        var line = MemoryRowBadge.Render(record, now, selected: false, maxWidth: 80, widths);

        // assert
        Assert.Equal("  fact ops now", Markup.Remove(line));
    }

    private static MemoryRecord CreateRecord(string type, IReadOnlyList<string> tags) => new()
    {
        Id = "m-1",
        Type = type,
        Tags = tags,
        Body = "Body.",
        CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        CreatedBy = "test-agent"
    };
}
