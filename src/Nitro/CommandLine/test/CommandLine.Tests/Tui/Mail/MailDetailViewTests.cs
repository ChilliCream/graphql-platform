using ChilliCream.Nitro.CommandLine.Tui.Mail;
using Spectre.Console;
using Spectre.Console.Testing;
using static ChilliCream.Nitro.CommandLine.Tests.Tui.AnsiAssertions;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailDetailViewTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RenderThread_Should_ShowSenderAndBody_When_OneMessageIsGiven()
    {
        // arrange
        var messages = new[]
        {
            MailMessageBuilder.Create("m-1", sender: "bob", subject: "Status update", body: "Hello there.", createdAt: s_now)
        };
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.RenderThread(messages, 80, 20, focused: true));

        // assert
        Assert.Contains("Thread: Status update", console.Output);
        Assert.Contains("bob", console.Output);
        Assert.Contains("Hello there.", console.Output);
    }

    [Fact]
    public void RenderThread_Should_ShowNoMessages_When_ThreadIsEmpty()
    {
        // arrange
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.RenderThread([], 80, 20, focused: true));

        // assert
        Assert.Contains("No messages.", console.Output);
    }

    [Fact]
    public void RenderThread_Should_ShowEveryMessageOldestFirst_When_ThreadHasMultipleMessages()
    {
        // arrange
        var messages = new[]
        {
            MailMessageBuilder.Create("m-1", sender: "bob", body: "First.", createdAt: s_now),
            MailMessageBuilder.Create("m-2", sender: "carol", body: "Second.", threadId: "m-1", createdAt: s_now.AddMinutes(1))
        };
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.RenderThread(messages, 80, 20, focused: true));

        // assert
        var bobIndex = console.Output.IndexOf("bob", StringComparison.Ordinal);
        var carolIndex = console.Output.IndexOf("carol", StringComparison.Ordinal);
        Assert.True(bobIndex >= 0 && carolIndex > bobIndex);
        Assert.Contains("First.", console.Output);
        Assert.Contains("Second.", console.Output);
    }

    [Fact]
    public void RenderThread_Should_NotThrow_When_WidthOrHeightIsZero()
    {
        // arrange
        var messages = new[] { MailMessageBuilder.Create("m-1", createdAt: s_now) };
        var view = new MailDetailView();

        // act
        var exception = Record.Exception(() => view.RenderThread(messages, 0, 0, focused: true));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void RenderThread_Should_WordWrapLongBody_When_BodyExceedsTheWidth()
    {
        // arrange
        var body = string.Join(" ", Enumerable.Repeat("word", 200));
        var messages = new[] { MailMessageBuilder.Create("m-1", body: body, createdAt: s_now) };
        var view = new MailDetailView();
        var console = new TestConsole().Width(30).Height(40);

        // act
        var exception = Record.Exception(() => console.Write(view.RenderThread(messages, 30, 40, focused: true)));

        // assert
        Assert.Null(exception);
        Assert.Contains("word", console.Output);
    }

    [Fact]
    public void RenderThread_Should_AttributeEachSpeaker_When_HarnessLookupHasEntries()
    {
        // arrange
        var messages = new[]
        {
            MailMessageBuilder.Create("m-1", sender: "bob", body: "Hello.", createdAt: s_now),
            MailMessageBuilder.Create("m-2", sender: "carol", body: "Reply.", threadId: "m-1", createdAt: s_now.AddMinutes(1))
        };
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);
        var harnessesByName = new Dictionary<string, string> { ["bob"] = "Codex", ["carol"] = "Claude Code" };

        // act
        console.Write(view.RenderThread(messages, 80, 20, focused: true, harnessesByName));

        // assert
        Assert.Contains("bob (Codex)", console.Output);
        Assert.Contains("carol (Claude Code)", console.Output);
    }

    [Fact]
    public void RenderThread_Should_ShowNoAttribution_When_HarnessIsEmptyOrLookupHasNoEntry()
    {
        // arrange
        var messages = new[] { MailMessageBuilder.Create("m-1", sender: "bob", createdAt: s_now) };
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);
        var harnessesByName = new Dictionary<string, string> { ["bob"] = "" };

        // act
        console.Write(view.RenderThread(messages, 80, 20, focused: true, harnessesByName));
        var actual = (
            HasName: console.Output.Contains("bob", StringComparison.Ordinal),
            HasAttribution: console.Output.Contains("bob (", StringComparison.Ordinal));

        // assert
        Assert.Equal((true, false), actual);
    }

    [Fact]
    public void RenderThread_Should_StayWithinPaneHeight_When_ThreadHasManyMessages()
    {
        // arrange
        var messages = Enumerable.Range(0, 20)
            .Select(i => MailMessageBuilder.Create($"m-{i}", threadId: "m-0", createdAt: s_now.AddMinutes(i)))
            .ToArray();
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        var exception = Record.Exception(() => console.Write(view.RenderThread(messages, 80, 20, focused: true)));

        // assert
        Assert.Null(exception);
        var lineCount = console.Output.Split('\n').Length;
        Assert.True(lineCount <= 21, $"Expected the panel to stay within its height budget, but got {lineCount} lines.");
    }

    [Fact]
    public void RenderThread_Should_ApplyAnsiStyling_When_RenderingTheSpeakerHeader()
    {
        // arrange
        var messages = new[] { MailMessageBuilder.Create("m-1", sender: "bob", createdAt: s_now) };
        var view = new MailDetailView();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(80).Height(20);

        // act
        console.Write(view.RenderThread(messages, 80, 20, focused: true));

        // assert
        AssertAnsiStylePrefixesText(console.Output, "detail.section.header", "bob");
    }

    [Fact]
    public void ScrollDown_And_ScrollUp_Should_NotThrow_When_CalledBeforeAnyRender()
    {
        // arrange
        var view = new MailDetailView();

        // act
        var exception = Record.Exception(() =>
        {
            view.ScrollDown();
            view.ScrollUp();
        });

        // assert
        Assert.Null(exception);
    }
}
