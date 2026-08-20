using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// Exercises <see cref="MailMode"/> and its overlays against a real
/// <see cref="MailStore"/>/SQLite workspace, the same way
/// <c>MailStoreTests</c> does for the store itself. <see cref="FakeMailStore"/>
/// filters and mutates through the production <see cref="MailRecipientView"/>
/// helper, so it cannot independently prove the real store's SQL-level
/// recipient validation and reply-recipient computation; these tests fill
/// that gap.
/// </summary>
public sealed class MailModeRealStoreTests : IAsyncDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _workingDirectory;
    private readonly string _workspaceDirectory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly MailStore _store;

    public MailModeRealStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-mode-tests");
        _workingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(_workingDirectory);
        _workspaceDirectory = AgentWorkspace.GetDirectory(_workingDirectory);

        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _store = new MailStore(new TestFileSystem(_workingDirectory), _timeProvider, new AgentDatabase());
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
        _tempRoot.Delete(recursive: true);
    }

    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo CtrlKey(ConsoleKey key) => new('\0', key, false, false, true);

    private static void Type(MailMode mode, string text)
    {
        foreach (var c in text)
        {
            mode.HandleRawKey(Key(c));
        }
    }

    private async Task InitAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_workspaceDirectory);
        await _store.InitializeWorkspaceAsync(_workspaceDirectory, cancellationToken);
    }

    private MailMode CreateMode(string actor) => new(_store, actor, _timeProvider);

    [Fact]
    public async Task OpenSelected_Should_MarkMessageRead_AgainstTheRealStore()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitAsync(cancellationToken);
        await _store.RegisterAgentAsync("alice", cancellationToken);
        await _store.RegisterAgentAsync("bob", cancellationToken);
        var sent = await _store.SendMessageAsync(
            new MailMessageCreation { Sender = "bob", Subject = "Hi", Body = "Body", To = ["alice"] },
            cancellationToken);

        var mode = CreateMode("alice");
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.OpenSelected());

        // assert
        var reloaded = await _store.GetRequiredMessageAsync(sent.Id, cancellationToken);
        var recipient = MailRecipientView.FindRecipient(reloaded, "alice");
        Assert.NotNull(recipient!.ReadAt);
    }

    [Fact]
    public async Task ArchiveConfirmation_Confirmed_Should_ArchiveMessage_AgainstTheRealStore()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitAsync(cancellationToken);
        await _store.RegisterAgentAsync("alice", cancellationToken);
        await _store.RegisterAgentAsync("bob", cancellationToken);
        var sent = await _store.SendMessageAsync(
            new MailMessageCreation { Sender = "bob", Subject = "Hi", Body = "Body", To = ["alice"] },
            cancellationToken);

        var mode = CreateMode("alice");
        mode.OnEnter();
        mode.Handle(new TuiMessage.ArchiveRequested());

        // act
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Enter));

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Success, Assert.IsType<TuiMessage.ShowToast>(toast).Style);

        var reloaded = await _store.GetRequiredMessageAsync(sent.Id, cancellationToken);
        var recipient = MailRecipientView.FindRecipient(reloaded, "alice");
        Assert.NotNull(recipient!.ArchivedAt);

        var defaultInbox = await _store.QueryInboxAsync(
            new MailInboxFilter { Actor = "alice" }, cancellationToken);
        Assert.Empty(defaultInbox);
    }

    [Fact]
    public async Task ComposeForm_Submit_Should_ShowErrorToast_And_WriteNothing_When_RecipientIsUnknown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitAsync(cancellationToken);
        await _store.RegisterAgentAsync("alice", cancellationToken);

        var mode = CreateMode("alice");
        mode.OnEnter();
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "ghost");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");

        // act
        var followUp = mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert: the store's unknown-recipient ExitException surfaces as an
        // error toast, the compose form stays open with its values intact,
        // and nothing was written.
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Error, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.True(mode.IsInputCapturing);

        var inbox = await _store.QueryInboxAsync(
            new MailInboxFilter { Actor = "alice", IncludeArchived = true }, cancellationToken);
        Assert.Empty(inbox);
    }

    [Fact]
    public async Task ReplyForm_Submit_Should_ComputeTheSameRecipientSet_AsTheCliReplyCommand()
    {
        // arrange: two equivalent threads, one replied to the way the CLI's
        // reply command would (calling IMailStore.ReplyMessageAsync
        // directly), one replied to through MailReplyForm. Since the form
        // calls the exact same store member with the same arguments, no
        // separate TUI recipient computation exists to diverge from it.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitAsync(cancellationToken);
        await _store.RegisterAgentAsync("alice", cancellationToken);
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await _store.RegisterAgentAsync("carol", cancellationToken);

        var cliOriginal = await _store.SendMessageAsync(
            new MailMessageCreation { Sender = "bob", Subject = "Plan", Body = "Body", To = ["alice"], Cc = ["carol"] },
            cancellationToken);
        var tuiOriginal = await _store.SendMessageAsync(
            new MailMessageCreation { Sender = "bob", Subject = "Plan", Body = "Body", To = ["alice"], Cc = ["carol"] },
            cancellationToken);

        // act
        var cliReply = await _store.ReplyMessageAsync(cliOriginal.Id, "alice", "CLI reply", cancellationToken);

        var replyForm = new MailReplyForm(tuiOriginal);
        var values = new Dictionary<string, FormValue> { [MailReplyForm.BodyFieldId] = new FormValue.Text("TUI reply") };
        var outcome = await replyForm.SubmitAsync(_store, values, "alice", cancellationToken);
        var succeeded = Assert.IsType<MailSendOutcome.Succeeded>(outcome);
        var tuiReply = await _store.GetRequiredMessageAsync(succeeded.MessageId, cancellationToken);

        // assert
        var cliRecipientNames = cliReply.Recipients.Select(r => r.Name).OrderBy(n => n, StringComparer.Ordinal);
        var tuiRecipientNames = tuiReply.Recipients.Select(r => r.Name).OrderBy(n => n, StringComparer.Ordinal);
        Assert.Equal(cliRecipientNames, tuiRecipientNames);
        Assert.Equal(["bob", "carol"], cliRecipientNames);
    }
}
