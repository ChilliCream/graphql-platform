using ChilliCream.Nitro.CommandLine.Tui.Agents;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

public sealed class AgentSentMailRowBadgeTests
{
    [Fact]
    public void Render_Should_FitDisplayWidth_When_FixedPrefixExceedsMaxWidth()
    {
        // arrange
        const int maxWidth = 1;
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var message = MailMessageBuilder.Create("m-1", createdAt: now);

        // act
        var line = AgentSentMailRowBadge.Render(message, now, maxWidth);

        // assert
        Assert.True(Markup.Remove(line).GetCellWidth() <= maxWidth);
        Assert.Equal("…", Markup.Remove(line));
    }

    [Fact]
    public void Render_Should_FitDisplayWidth_When_SubjectAndRecipientsContainCjkAndEmoji()
    {
        // arrange
        const int maxWidth = 24;
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var message = MailMessageBuilder.Create(
            "m-1",
            subject: "漢😀 subject text",
            createdAt: now,
            recipients: [MailMessageBuilder.ToRecipient("漢😀 recipient")]);

        // act
        var line = AgentSentMailRowBadge.Render(message, now, maxWidth);

        // assert
        Assert.True(Markup.Remove(line).GetCellWidth() <= maxWidth);
    }
}
