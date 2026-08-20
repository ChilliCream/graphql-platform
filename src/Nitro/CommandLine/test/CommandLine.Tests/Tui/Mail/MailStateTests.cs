using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailStateTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MailState CreateState(FakeMailStore store, string actor = "alice")
        => new(actor, new MailDataLoader(store));

    [Fact]
    public async Task RefreshAsync_Should_LoadInboxForTheCurrentFilter()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);

        // act
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["m-1"], state.Messages.Select(m => m.Id));
    }

    [Fact]
    public async Task RefreshAsync_Should_KeepSelectedRowOnSameMessage_When_MessageStillPresentAfterReorder()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        state.SelectedRow = 1; // m-1, currently the older/last row

        // act: a newer message pushes m-1 to a different row on refresh
        store.Messages.Add(MailMessageBuilder.Create(
            "m-3", createdAt: Now.AddMinutes(2), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["m-3", "m-2", "m-1"], state.Messages.Select(m => m.Id));
        Assert.Equal(2, state.SelectedRow);
        Assert.Equal("m-1", state.SelectedMessage?.Id);
    }

    [Fact]
    public async Task RefreshAsync_Should_ClampSelectedRow_When_SelectedMessageNoLongerPresent()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        state.SelectedRow = 0; // m-2, newest first

        // act: m-2 is removed from the store entirely
        store.Messages.RemoveAt(1);
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["m-1"], state.Messages.Select(m => m.Id));
        Assert.Equal(0, state.SelectedRow);
    }

    [Fact]
    public async Task CycleFilterAsync_Should_AdvanceThroughFiltersAndWrap()
    {
        // arrange
        var store = new FakeMailStore();
        var state = CreateState(store);
        Assert.Equal(MailListFilter.Inbox, state.Filter);

        // act & assert: Inbox -> Unread -> Archived -> Inbox
        await state.CycleFilterAsync(1, CancellationToken.None);
        Assert.Equal(MailListFilter.Unread, state.Filter);

        await state.CycleFilterAsync(1, CancellationToken.None);
        Assert.Equal(MailListFilter.Archived, state.Filter);

        await state.CycleFilterAsync(1, CancellationToken.None);
        Assert.Equal(MailListFilter.Inbox, state.Filter);
    }

    [Fact]
    public async Task CycleFilterAsync_Should_WrapBackward_When_DeltaIsNegative()
    {
        // arrange
        var store = new FakeMailStore();
        var state = CreateState(store);

        // act
        await state.CycleFilterAsync(-1, CancellationToken.None);

        // assert
        Assert.Equal(MailListFilter.Archived, state.Filter);
    }

    [Fact]
    public async Task ShowThreadAsync_Should_LoadSelectedMessagesThread()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: Now.AddMinutes(1)));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);

        // act
        var opened = await state.ShowThreadAsync(CancellationToken.None);

        // assert
        Assert.True(opened);
        Assert.Equal(MailViewMode.Thread, state.ViewMode);
        Assert.Equal(["m-1", "m-2"], state.ThreadMessages.Select(m => m.Id));
    }

    [Fact]
    public async Task ShowThreadAsync_Should_ReturnFalse_When_NoMessageSelected()
    {
        // arrange
        var store = new FakeMailStore();
        var state = CreateState(store);

        // act
        var opened = await state.ShowThreadAsync(CancellationToken.None);

        // assert
        Assert.False(opened);
        Assert.Equal(MailViewMode.Message, state.ViewMode);
    }

    [Fact]
    public async Task ShowMessage_Should_ClearThreadMessagesAndSwitchBack()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        await state.ShowThreadAsync(CancellationToken.None);

        // act
        state.ShowMessage();

        // assert
        Assert.Equal(MailViewMode.Message, state.ViewMode);
        Assert.Empty(state.ThreadMessages);
    }

    [Fact]
    public void SelectedMessage_Should_ReturnNull_When_MessagesIsEmpty()
    {
        // arrange
        var store = new FakeMailStore();
        var state = CreateState(store);

        // act & assert
        Assert.Null(state.SelectedMessage);
    }
}
