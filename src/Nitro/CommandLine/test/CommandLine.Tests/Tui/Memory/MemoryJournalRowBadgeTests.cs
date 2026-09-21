using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Memory;

public sealed class MemoryJournalRowBadgeTests
{
    [Fact]
    public void Render_Should_FitDisplayWidth_When_PreviewContainsCjkAndEmoji()
    {
        // arrange
        const int maxWidth = 21;
        var entry = CreateEntry("😀漢 preview");
        var widths = MemoryJournalRowBadge.ComputeWidths([entry]);

        // act
        var line = MemoryJournalRowBadge.Render(entry, selected: false, maxWidth, widths);
        var plainText = Markup.Remove(line);

        // assert
        Assert.Equal("  2026-01-01 00:00 …", plainText);
        Assert.True(plainText.GetCellWidth() <= maxWidth);
    }

    [Fact]
    public void Render_Should_FitFixedColumns_When_MaxWidthMatchesTheirDisplayWidth()
    {
        // arrange
        const int maxWidth = 19;
        var entry = CreateEntry("Preview");
        var widths = MemoryJournalRowBadge.ComputeWidths([entry]);

        // act
        var line = MemoryJournalRowBadge.Render(entry, selected: false, maxWidth, widths);
        var plainText = Markup.Remove(line);

        // assert
        Assert.Equal("  2026-01-01 00:00 ", plainText);
        Assert.Equal(maxWidth, plainText.GetCellWidth());
    }

    [Fact]
    public void Render_Should_FitWithinOneColumn_When_Selected()
    {
        // arrange
        var entry = CreateEntry("😀 preview");
        var widths = MemoryJournalRowBadge.ComputeWidths([entry]);

        // act
        var line = MemoryJournalRowBadge.Render(entry, selected: true, maxWidth: 1, widths);
        var plainText = Markup.Remove(line);

        // assert
        Assert.Equal(">", plainText);
    }

    [Fact]
    public void Render_Should_PreserveNormalOutput_When_ItFits()
    {
        // arrange
        var entry = CreateEntry("Preview");
        var widths = MemoryJournalRowBadge.ComputeWidths([entry]);

        // act
        var line = MemoryJournalRowBadge.Render(entry, selected: false, maxWidth: 80, widths);

        // assert
        Assert.Equal("  2026-01-01 00:00 Preview", Markup.Remove(line));
    }

    private static MemoryJournalEntry CreateEntry(string body) => new()
    {
        Id = "j-1",
        Body = body,
        CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        CreatedBy = "test-agent"
    };
}
