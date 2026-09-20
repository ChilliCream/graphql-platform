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
/// Exercises <see cref="MailMode"/> and its overlays against a real <see cref="MailStore"/>/SQLite
/// workspace.
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

    private MailMode CreateMode(string actor) => new(_store, actor, _registry, _timeProvider);

    /// <summary>
    /// Refreshes <see cref="MailMode"/> until a non-<see cref="ToastStyle.Info"/> outcome toast
    /// is returned. Cancels the wait after five seconds or when the supplied token is cancelled.
    /// </summary>
    private static async Task<TuiMessage.ShowToast> WaitForOutcomeToastAsync(
        MailMode mode, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        while (true)
        {
            var result = mode.Handle(new TuiMessage.RefreshRequested());

            foreach (var message in result)
            {
                var toast = Assert.IsType<TuiMessage.ShowToast>(message);

                if (toast.Style != ToastStyle.Info)
                {
                    return toast;
                }
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
        // arrange
        // Compose a message to an unregistered recipient.
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
        // arrange
        // Seed matching threads for a direct reply and a reply built by the form.
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
