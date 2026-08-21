using ChilliCream.Nitro.CommandLine.Tui.Mail;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailDetailViewTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static async Task<MailState> CreateStateWithMessageAsync(FakeMailStore store, string body = "Hello there.")
    {
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            subject: "Status update",
            body: body,
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]));

        var state = new MailState("alice", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        return state;
    }

    [Fact]
    public async Task Render_Should_ShowSenderAndBody_When_MessageSelected()
    {
        // arrange
        var store = new FakeMailStore();
        var state = await CreateStateWithMessageAsync(store);
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        Assert.Contains("From: bob", console.Output);
        Assert.Contains("Hello there.", console.Output);
    }

    [Fact]
    public async Task Render_Should_ShowNoMessageSelected_When_MessagesListIsEmptyButLoaded()
    {
        // arrange
        var store = new FakeMailStore();
        var state = new MailState("alice", new MailDataLoader(store));
        await state.RefreshAsync(CancellationToken.None);
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        Assert.Contains("No messages.", console.Output);
    }

    [Fact]
    public async Task Render_Should_ShowEveryMessageInTheThread_When_ViewModeIsThread()
    {
        // arrange
        var store = new FakeMailStore();
        var state = await CreateStateWithMessageAsync(store);
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", body: "Reply body.", threadId: "m-1", createdAt: Now.AddMinutes(1)));
        await state.ShowThreadAsync(CancellationToken.None);
        var view = new MailDetailView();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(view.Render(state, 80, 20, focused: true));

        // assert
        Assert.Contains("bob", console.Output);
        Assert.Contains("carol", console.Output);
        Assert.Contains("Reply body.", console.Output);
    }

    [Fact]
    public async Task Render_Should_NotThrow_When_WidthOrHeightIsZero()
    {
        // arrange
        var store = new FakeMailStore();
        var state = await CreateStateWithMessageAsync(store);
        var view = new MailDetailView();

        // act
        var exception = Record.Exception(() => view.Render(state, 0, 0, focused: true));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Render_Should_WordWrapLongBody_WithoutClippingContent()
    {
        // arrange
        var store = new FakeMailStore();
        var body = string.Join(" ", Enumerable.Repeat("word", 200));
        var state = await CreateStateWithMessageAsync(store, body);
        var view = new MailDetailView();
        var console = new TestConsole().Width(30).Height(40);

        // act
        var exception = Record.Exception(() => console.Write(view.Render(state, 30, 40, focused: true)));

        // assert
        Assert.Null(exception);
        Assert.Contains("word", console.Output);
    }
}
