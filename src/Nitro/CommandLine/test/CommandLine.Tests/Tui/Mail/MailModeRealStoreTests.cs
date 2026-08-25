using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
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
    private readonly AgentRegistry _registry;
    private readonly MailStore _store;

    public MailModeRealStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-mode-tests");
        _workingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(_workingDirectory);
        _workspaceDirectory = AgentWorkspace.GetDirectory(_workingDirectory);

        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _registry = new AgentRegistry(new TestFileSystem(_workingDirectory), _timeProvider, new AgentDatabase());

        // The instance id and global config directory providers are only
        // required for MailWakePolicy.Enqueue (see MailStore.SendMessageAsync/
        // ReplyMessageAsync's remarks); MailMode always sends and replies
        // with Enqueue, matching the CLI's own send/reply commands.
        _store = new MailStore(
            new TestFileSystem(_workingDirectory),
            _timeProvider,
            new AgentDatabase(),
            _registry,
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_workingDirectory));
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

    private MailMode CreateMode(string actor, FakeMailWakeReceiptObserver? wakeObserver = null) => new(
        _store,
        actor,
        _registry,
        new DaemonOwnedActorWakeDispatcher(),
        wakeObserver ?? new FakeMailWakeReceiptObserver(),
        _timeProvider);

    /// <summary>
    /// Polls <see cref="MailMode.Handle"/> with a <see cref="TuiMessage.RefreshRequested"/>
    /// until a compose/reply outcome toast drains; see the identical helper
    /// in <c>MailModeTests</c>.
    /// </summary>
    private static async Task<TuiMessage.ShowToast> WaitForOutcomeToastAsync(
        MailMode mode, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        while (true)
        {
            var result = mode.Handle(new TuiMessage.RefreshRequested());

            if (result.Count > 0)
            {
                return Assert.IsType<TuiMessage.ShowToast>(Assert.Single(result));
            }

            await Task.Delay(5, timeoutCts.Token);
        }
    }

    [Fact]
    public async Task OpenSelected_Should_MarkMessageRead_AgainstTheRealStore()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitAsync(cancellationToken);
        await _registry.RegisterAsync("alice", role: "", client: "", cancellationToken);
        await _registry.RegisterAsync("bob", role: "", client: "", cancellationToken);
        var sent = await _store.SendMessageAsync(
            new MailMessageCreation { Sender = "bob", Subject = "Hi", Body = "Body", To = ["alice"] },
            cancellationToken);

        var mode = CreateMode("alice");
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only

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
        await _registry.RegisterAsync("alice", role: "", client: "", cancellationToken);
        await _registry.RegisterAsync("bob", role: "", client: "", cancellationToken);
        var sent = await _store.SendMessageAsync(
            new MailMessageCreation { Sender = "bob", Subject = "Hi", Body = "Body", To = ["alice"] },
            cancellationToken);

        var mode = CreateMode("alice");
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
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
    public async Task ComposeForm_Submit_Should_CreateImplicitRow_When_RecipientIsUnknown()
    {
        // arrange: bd-agent-unify-814.9 replaced the store's hard fail on an
        // unknown recipient with mailbox-on-first-message: the send now
        // succeeds and implicit-creates the recipient's agent row, so the
        // compose form (which has no client-side recipient check of its own
        // and only surfaces the store's ExitException) now sees the write
        // itself succeed (the wake status, observed Pending here by the
        // default fake observer, is reported separately, truthfully, not
        // rolled into whether the store write itself succeeded).
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitAsync(cancellationToken);
        await _registry.RegisterAsync("alice", role: "", client: "", cancellationToken);

        var mode = CreateMode("alice");
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "ghost");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        await WaitForOutcomeToastAsync(mode, cancellationToken);

        // assert
        var ghost = await _registry.GetAsync("ghost", cancellationToken);
        Assert.True(ghost?.Implicit);
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
        await _registry.RegisterAsync("alice", role: "", client: "", cancellationToken);
        await _registry.RegisterAsync("bob", role: "", client: "", cancellationToken);
        await _registry.RegisterAsync("carol", role: "", client: "", cancellationToken);

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
        var request = replyForm.BuildRequest(values, "alice");
        var tuiReply = await _store.ReplyMessageAsync(
            request.InReplyToId, request.Actor, request.Body, MailWakePolicy.Enqueue, cancellationToken);

        // assert
        var cliRecipientNames = cliReply.Recipients.Select(r => r.Name).OrderBy(n => n, StringComparer.Ordinal);
        var tuiRecipientNames = tuiReply.Recipients.Select(r => r.Name).OrderBy(n => n, StringComparer.Ordinal);
        Assert.Equal(cliRecipientNames, tuiRecipientNames);
        Assert.Equal(["bob", "carol"], cliRecipientNames);
    }
}
